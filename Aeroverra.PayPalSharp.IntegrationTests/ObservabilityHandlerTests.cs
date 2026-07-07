using System.Net;
using Aeroverra.PayPalSharp;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// The OnRequest/OnResponse callbacks fire (giving access to success-path headers like PayPal-Debug-Id),
/// and a throwing callback never breaks the call. No network.
/// </summary>
public class ObservabilityHandlerTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.TryAddWithoutValidation(PayPalHeaders.DebugIdHeader, "abc123debug");
            return Task.FromResult(response);
        }
    }

    private static HttpMessageInvoker Build(PayPalOptions options)
        => new(new PayPalObservabilityHandler(Options.Create(options)) { InnerHandler = new StubHandler() });

    [Fact]
    public async Task Callbacks_fire_and_expose_the_debug_id_on_success()
    {
        HttpRequestMessage? seenRequest = null;
        string? debugId = null;

        var invoker = Build(new PayPalOptions
        {
            OnRequest = req => seenRequest = req,
            OnResponse = res => debugId = PayPalHeaders.GetDebugId(res),
        });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), CancellationToken.None);

        Assert.NotNull(seenRequest);
        Assert.Equal("abc123debug", debugId); // debug id readable on a successful response
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("connection reset");
    }

    [Fact]
    public async Task OnException_fires_on_a_transport_failure_and_OnResponse_does_not()
    {
        Exception? seen = null;
        var responseFired = false;
        var options = Options.Create(new PayPalOptions
        {
            OnException = (_, ex) => seen = ex,
            OnResponse = _ => responseFired = true,
        });
        using var invoker = new HttpMessageInvoker(new PayPalObservabilityHandler(options) { InnerHandler = new ThrowingHandler() });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), CancellationToken.None));

        Assert.IsType<HttpRequestException>(seen);   // OnException got the transport error
        Assert.False(responseFired);                 // OnResponse did not fire (no response)
    }

    [Fact]
    public async Task A_throwing_callback_does_not_break_the_request()
    {
        var invoker = Build(new PayPalOptions
        {
            OnRequest = _ => throw new InvalidOperationException("boom"),
            OnResponse = _ => throw new InvalidOperationException("boom"),
        });

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
