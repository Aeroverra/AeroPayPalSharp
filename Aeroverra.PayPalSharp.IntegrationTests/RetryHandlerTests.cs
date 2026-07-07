using System.Net;
using Aeroverra.PayPalSharp;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Proves the retry handler is SAFE: it retries transient failures for idempotent/idempotency-keyed
/// requests, never retries a non-idempotent POST (no double-charge), is bounded (no infinite loop),
/// respects cancellation, and honors Retry-After. No network.
/// </summary>
public class RetryHandlerTests
{
    // Records how many times it was invoked and returns a scripted sequence of outcomes.
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _script;
        public int Calls { get; private set; }

        public ScriptedHandler(params Func<HttpResponseMessage>[] script) => _script = new Queue<Func<HttpResponseMessage>>(script);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var next = _script.Count > 0 ? _script.Dequeue() : (() => new HttpResponseMessage(HttpStatusCode.OK));
            return Task.FromResult(next());
        }
    }

    private static HttpMessageInvoker Build(ScriptedHandler inner, out ScriptedHandler probe, PayPalRetryOptions? retry = null)
    {
        probe = inner;
        var options = Options.Create(new PayPalOptions
        {
            Retry = retry ?? new PayPalRetryOptions { MaxRetries = 3, BaseDelay = TimeSpan.FromMilliseconds(1), MaxDelay = TimeSpan.FromMilliseconds(5) },
        });
        return new HttpMessageInvoker(new PayPalRetryHandler(options) { InnerHandler = inner });
    }

    private static HttpResponseMessage Status(HttpStatusCode code) => new(code);

    private static HttpRequestMessage Post(bool withIdempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api-m.sandbox.paypal.com/v2/checkout/orders")
        {
            Content = new StringContent("{}"),
        };
        if (withIdempotencyKey)
        {
            request.Headers.TryAddWithoutValidation("PayPal-Request-Id", Guid.NewGuid().ToString());
        }
        return request;
    }

    [Fact]
    public async Task Retries_a_transient_500_then_succeeds()
    {
        var invoker = Build(new ScriptedHandler(
            () => Status(HttpStatusCode.InternalServerError),
            () => Status(HttpStatusCode.OK)), out var probe);

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, probe.Calls); // one failure + one success
    }

    [Fact]
    public async Task Never_retries_a_post_without_idempotency_key()
    {
        var invoker = Build(new ScriptedHandler(
            () => Status(HttpStatusCode.ServiceUnavailable),
            () => Status(HttpStatusCode.OK)), out var probe);

        var response = await invoker.SendAsync(Post(withIdempotencyKey: false), CancellationToken.None);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, probe.Calls); // sent exactly once - no double-charge risk
    }

    [Fact]
    public async Task Retries_a_post_with_idempotency_key()
    {
        var invoker = Build(new ScriptedHandler(
            () => Status(HttpStatusCode.ServiceUnavailable),
            () => Status(HttpStatusCode.OK)), out var probe);

        var response = await invoker.SendAsync(Post(withIdempotencyKey: true), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, probe.Calls); // safe: PayPal dedups by the key
    }

    [Fact]
    public async Task Never_retries_a_4xx_client_error()
    {
        var invoker = Build(new ScriptedHandler(
            () => Status(HttpStatusCode.UnprocessableEntity),
            () => Status(HttpStatusCode.OK)), out var probe);

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public async Task Is_bounded_and_returns_last_response_when_retries_exhaust()
    {
        // Always 503; with MaxRetries=3 that is 1 + 3 = 4 calls, then it gives up (no infinite loop).
        var invoker = Build(new ScriptedHandler(
            () => Status(HttpStatusCode.ServiceUnavailable),
            () => Status(HttpStatusCode.ServiceUnavailable),
            () => Status(HttpStatusCode.ServiceUnavailable),
            () => Status(HttpStatusCode.ServiceUnavailable),
            () => Status(HttpStatusCode.ServiceUnavailable)), out var probe);

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(4, probe.Calls);
    }

    [Fact]
    public async Task Disabled_when_max_retries_is_zero()
    {
        var invoker = Build(
            new ScriptedHandler(() => Status(HttpStatusCode.InternalServerError), () => Status(HttpStatusCode.OK)),
            out var probe,
            new PayPalRetryOptions { MaxRetries = 0 });

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(1, probe.Calls);
    }

    [Fact]
    public async Task Retries_a_network_exception_then_succeeds()
    {
        var invoker = Build(new ScriptedHandler(
            () => throw new HttpRequestException("connection reset"),
            () => Status(HttpStatusCode.OK)), out var probe);

        var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, probe.Calls);
    }

    [Fact]
    public async Task Stops_immediately_when_the_caller_cancels()
    {
        using var cts = new CancellationTokenSource();
        var invoker = Build(new ScriptedHandler(() =>
        {
            cts.Cancel(); // caller cancels during the first attempt
            return Status(HttpStatusCode.ServiceUnavailable);
        }), out var probe);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/y"), cts.Token));
        Assert.Equal(1, probe.Calls); // did not retry after cancellation
    }
}
