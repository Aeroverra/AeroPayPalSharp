using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Aeroverra.PayPalSharp;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Deterministic, no-network tests for the offline webhook verifier: the CRC-32 matches the
/// standard vector, a correctly-signed payload passes, and tampering / wrong id / missing headers
/// / untrusted cert host all fail as expected. The signature here is produced with a throwaway
/// self-signed cert so the whole thing is self-contained.
/// </summary>
public class WebhookVerifierTests
{
    private const string WebhookId = "WH-TEST-1234567890";
    private const string TransmissionId = "11111111-2222-3333-4444-555555555555";
    private const string TransmissionTime = "2026-05-15T20:41:42Z";
    private const string CertUrl = "https://api.sandbox.paypal.com/v1/notifications/certs/CERT-test";

    private sealed class StubCertSource(X509Certificate2 certificate) : IPayPalCertificateSource
    {
        public Task<X509Certificate2> GetAsync(Uri certificateUrl, CancellationToken cancellationToken = default)
            => Task.FromResult(certificate);
    }

    private static (PayPalWebhookVerifier verifier, RSA rsa, X509Certificate2 cert) BuildVerifier()
    {
        var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=aero-paypal-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var verifier = new PayPalWebhookVerifier(new StubCertSource(cert), Options.Create(new PayPalOptions { WebhookId = WebhookId }));
        return (verifier, rsa, cert);
    }

    private static string Sign(RSA rsa, string body)
    {
        var crc = PayPalCrc32.Compute(Encoding.UTF8.GetBytes(body));
        var signed = $"{TransmissionId}|{TransmissionTime}|{WebhookId}|{crc}";
        var sig = rsa.SignData(Encoding.UTF8.GetBytes(signed), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(sig);
    }

    private static List<KeyValuePair<string, string>> Headers(string sig) => new()
    {
        new("paypal-transmission-id", TransmissionId),
        new("paypal-transmission-time", TransmissionTime),
        new("paypal-cert-url", CertUrl),
        new("paypal-auth-algo", "SHA256withRSA"),
        new("paypal-transmission-sig", sig),
    };

    [Fact]
    public void Crc32_matches_the_standard_test_vector()
    {
        // The canonical CRC-32 of "123456789" is 0xCBF43926.
        Assert.Equal(0xCBF43926u, PayPalCrc32.Compute(Encoding.ASCII.GetBytes("123456789")));
    }

    [Fact]
    public async Task Valid_signature_passes()
    {
        var (verifier, rsa, _) = BuildVerifier();
        var body = "{\"id\":\"WH-EVT-1\",\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}";
        var result = await verifier.VerifyAsync(Headers(Sign(rsa, body)), body);
        Assert.True(result.IsValid, result.FailureReason);
    }

    [Fact]
    public async Task Tampered_body_fails()
    {
        var (verifier, rsa, _) = BuildVerifier();
        var body = "{\"id\":\"WH-EVT-1\",\"amount\":\"10.00\"}";
        var sig = Sign(rsa, body);
        var tampered = body.Replace("10.00", "9999.00");
        var result = await verifier.VerifyAsync(Headers(sig), tampered);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Wrong_webhook_id_fails()
    {
        var (verifier, rsa, _) = BuildVerifier();
        var body = "{\"id\":\"WH-EVT-1\"}";
        var result = await verifier.VerifyAsync(Headers(Sign(rsa, body)), body, webhookId: "SOME-OTHER-WEBHOOK-ID");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Missing_headers_fails_gracefully()
    {
        var (verifier, _, _) = BuildVerifier();
        var result = await verifier.VerifyAsync(new List<KeyValuePair<string, string>>(), "{}");
        Assert.False(result.IsValid);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public async Task Certificate_source_rejects_untrusted_host()
    {
        var factory = new SingleClientFactory();
        var source = new HttpPayPalCertificateSource(factory);
        await Assert.ThrowsAsync<PayPalWebhookVerificationException>(
            () => source.GetAsync(new Uri("https://evil.example.com/v1/notifications/certs/CERT-x")));
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
