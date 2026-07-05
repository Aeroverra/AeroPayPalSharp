using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Deterministic (no network) tests that the ActingAsMerchant scope drives the
/// PayPal-Auth-Assertion header on and off, and that partner attribution is always sent.
/// </summary>
public class MerchantScopeTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Last = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static (HttpMessageInvoker invoker, CapturingHandler capturing, PayPalMerchantContext context) Build()
    {
        var context = new PayPalMerchantContext();
        var options = Options.Create(new PayPalOptions { ClientId = "CID", PartnerAttributionId = "BN123" });
        var capturing = new CapturingHandler();
        var handler = new PayPalPartnerHeaderHandler(options, context) { InnerHandler = capturing };
        return (new HttpMessageInvoker(handler), capturing, context);
    }

    [Fact]
    public async Task Partner_attribution_is_always_sent()
    {
        var (invoker, capturing, _) = Build();
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/"), default);
        Assert.True(capturing.Last!.Headers.Contains("PayPal-Partner-Attribution-Id"));
        Assert.False(capturing.Last!.Headers.Contains("PayPal-Auth-Assertion"));
    }

    [Fact]
    public async Task Auth_assertion_present_only_inside_ActingAsMerchant_scope()
    {
        var (invoker, capturing, context) = Build();

        using (context.ActingAs("MERCH123"))
        {
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/"), default);
        }
        Assert.True(capturing.Last!.Headers.Contains("PayPal-Auth-Assertion"));

        var assertion = capturing.Last!.Headers.GetValues("PayPal-Auth-Assertion").First();
        var payloadJson = Encoding.UTF8.GetString(FromBase64Url(assertion.Split('.')[1]));
        Assert.Contains("MERCH123", payloadJson); // payer_id
        Assert.Contains("CID", payloadJson);       // iss (client id)

        // After the scope ends the header is gone again.
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/"), default);
        Assert.False(capturing.Last!.Headers.Contains("PayPal-Auth-Assertion"));
    }

    private static byte[] FromBase64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
}
