using System.Security.Cryptography.X509Certificates;
using Aeroverra.PayPalSharp;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Verifies a REAL captured PayPal webhook (body + headers + signing cert) fully offline. This is the
/// test that proves our CRC-32 and signed-string construction match PayPal's, since the signature was
/// produced by PayPal.
///
/// It reads nothing account-specific from source: both the fixtures directory
/// (user-secret PayPal:WebhookFixturesDir) and the webhook id (PayPal:WebhookId) come from user-secrets,
/// so no real data or local paths live in this public repo. It skips cleanly when they are not set.
/// </summary>
[Collection(PayPalCollection.Name)]
public class WebhookRealFixtureTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public WebhookRealFixtureTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    private sealed class FixtureCertSource(X509Certificate2 certificate) : IPayPalCertificateSource
    {
        public Task<X509Certificate2> GetAsync(Uri certificateUrl, CancellationToken cancellationToken = default)
            => Task.FromResult(certificate);
    }

    [SkippableTheory]
    [InlineData("CHECKOUT_ORDER_APPROVED")]
    [InlineData("PAYMENT_CAPTURE_COMPLETED")]
    [InlineData("CUSTOMER_DISPUTE_CREATED")]
    public async Task Verifies_a_real_paypal_webhook(string fixture)
    {
        var fixturesDir = _fx.Configuration["PayPal:WebhookFixturesDir"];
        var webhookId = _fx.Configuration["PayPal:WebhookId"];
        Skip.If(string.IsNullOrWhiteSpace(fixturesDir) || !Directory.Exists(fixturesDir),
            "Set user-secret PayPal:WebhookFixturesDir to a folder of captured webhook fixtures to run this.");
        Skip.If(string.IsNullOrWhiteSpace(webhookId), "Set user-secret PayPal:WebhookId to the webhook subscription id the fixtures were sent to.");

        var bodyPath = Path.Combine(fixturesDir!, fixture + "_rawbody.json");
        var headersPath = Path.Combine(fixturesDir!, fixture + "_headers.json");
        Skip.IfNot(File.Exists(bodyPath) && File.Exists(headersPath), $"Fixture {fixture} not found.");

        var body = await File.ReadAllBytesAsync(bodyPath);
        var headers = LoadHeaders(headersPath);

        // The cert file is named after the id in the paypal-cert-url header.
        var certUrl = headers.First(h => h.Key.Equals("paypal-cert-url", StringComparison.OrdinalIgnoreCase)).Value;
        var certId = certUrl.TrimEnd('/').Split('/').Last();
        var certPath = Path.Combine(fixturesDir!, "sandbox-" + certId + ".pem");
        Skip.IfNot(File.Exists(certPath), $"Certificate {certId} not present.");

        var certificate = X509Certificate2.CreateFromPem(await File.ReadAllTextAsync(certPath));
        var verifier = new PayPalWebhookVerifier(
            new FixtureCertSource(certificate),
            Options.Create(new PayPalOptions { WebhookId = webhookId }));

        var result = await verifier.VerifyAsync(headers, body);

        Assert.True(result.IsValid, result.FailureReason);
        _output.WriteLine($"{fixture}: verified real PayPal signature offline");

        // And a one-byte tamper must fail.
        var tampered = (byte[])body.Clone();
        tampered[^2] ^= 0x01;
        var bad = await verifier.VerifyAsync(headers, tampered);
        Assert.False(bad.IsValid);
    }

    private static List<KeyValuePair<string, string>> LoadHeaders(string path)
    {
        // DateParseHandling.None so the ISO transmission-time header stays a verbatim string
        // (default parsing would rewrite "2026-05-15T20:41:42Z" and break the signed string).
        using var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(File.ReadAllText(path)))
        {
            DateParseHandling = Newtonsoft.Json.DateParseHandling.None,
        };
        var json = JObject.Load(reader);
        var list = new List<KeyValuePair<string, string>>();
        foreach (var prop in json.Properties())
        {
            var value = prop.Value is JArray arr ? arr.First?.ToString() : prop.Value.ToString();
            if (value is not null)
            {
                list.Add(new KeyValuePair<string, string>(prop.Name, value));
            }
        }
        return list;
    }
}
