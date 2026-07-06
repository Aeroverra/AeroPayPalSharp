using System.Net;
using System.Text;
using Aeroverra.PayPalSharp;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Proves the ActingAsMerchant scope is isolated per async flow when ONE shared client/handler/context
/// is hit from many threads at once: concurrent scopes never bleed into each other, a flow with no scope
/// sends no auth-assertion even while other flows are scoped, and nested scopes restore correctly. No
/// network - a single shared handler echoes back the PayPal-Auth-Assertion header it received.
/// </summary>
public class MerchantScopeConcurrencyTests
{
    private const string ClientId = "test-client-id";

    // One handler + one context shared across every task, exactly like a real pooled HttpClient handler.
    private sealed class EchoAssertionHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Yield(); // force a thread hop mid-pipeline
            request.Headers.TryGetValues("PayPal-Auth-Assertion", out var values);
            var assertion = values?.FirstOrDefault() ?? string.Empty;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(assertion) };
        }
    }

    private static string? PayerIdFromAssertion(string assertion)
    {
        if (string.IsNullOrEmpty(assertion))
        {
            return null;
        }
        var payload = assertion.Split('.')[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        return (string?)json["payer_id"];
    }

    [Fact]
    public async Task Concurrent_scopes_do_not_leak_across_flows()
    {
        var context = new PayPalMerchantContext();
        var options = Options.Create(new PayPalOptions { ClientId = ClientId, PartnerAttributionId = "BN-TEST" });
        using var invoker = new HttpMessageInvoker(new PayPalPartnerHeaderHandler(options, context) { InnerHandler = new EchoAssertionHandler() });

        async Task<string?> SendWithScope(string? merchantId)
        {
            IDisposable? scope = merchantId is null ? null : context.ActingAs(merchantId);
            try
            {
                await Task.Yield();                              // hop threads before sending
                await Task.Delay(Random.Shared.Next(0, 3));      // stagger so flows genuinely overlap
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api-m.sandbox.paypal.com/x");
                var response = await invoker.SendAsync(request, CancellationToken.None);
                await Task.Yield();
                return PayerIdFromAssertion(await response.Content.ReadAsStringAsync());
            }
            finally
            {
                scope?.Dispose();
            }
        }

        // 900 interleaved flows: a third scoped to A, a third to B, a third with no scope at all.
        var tasks = Enumerable.Range(0, 900).Select(i =>
        {
            var expected = (i % 3) switch { 0 => (string?)null, 1 => "MERCHANT_AAA", _ => "MERCHANT_BBB" };
            return (expected, task: SendWithScope(expected));
        }).ToList();

        await Task.WhenAll(tasks.Select(t => t.task));

        foreach (var (expected, task) in tasks)
        {
            Assert.Equal(expected, task.Result); // each flow saw its own merchant (or none), never another's
        }
    }

    [Fact]
    public async Task Nested_scopes_restore_the_outer_value_across_awaits()
    {
        var context = new PayPalMerchantContext();

        Assert.Null(context.CurrentMerchantId);
        using (context.ActingAs("OUTER"))
        {
            await Task.Yield();
            Assert.Equal("OUTER", context.CurrentMerchantId);
            using (context.ActingAs("INNER"))
            {
                await Task.Delay(1);
                Assert.Equal("INNER", context.CurrentMerchantId);
            }
            Assert.Equal("OUTER", context.CurrentMerchantId);
        }
        Assert.Null(context.CurrentMerchantId);
    }
}
