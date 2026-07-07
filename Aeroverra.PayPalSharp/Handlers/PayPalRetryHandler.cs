using System.Net;
using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Retries transient failures safely. Safety rules (so a payment is never applied twice, and there is no
/// unbounded loop or wait):
/// <list type="bullet">
/// <item>Only transient outcomes are retried: a network error, a timeout, or status 408/429/500/502/503/504.
/// A 4xx (other than 429) is a permanent client error and is never retried.</item>
/// <item>A request is only retried when re-sending it is safe: idempotent methods (GET/HEAD/OPTIONS/DELETE/PUT),
/// or POST/PATCH ONLY when it carries a <c>PayPal-Request-Id</c> idempotency key (PayPal then deduplicates,
/// so it cannot be applied twice). A POST/PATCH without that key is never retried.</item>
/// <item>Attempts are hard-capped by <see cref="PayPalRetryOptions.MaxRetries"/> (no infinite loop). Each
/// backoff wait is exponential with jitter and capped by <see cref="PayPalRetryOptions.MaxDelay"/>; a server
/// <c>Retry-After</c> is honored but also capped (no unbounded wait). The caller's timeout/cancellation is
/// always respected and stops retrying immediately.</item>
/// </list>
/// This handler is outermost, so each attempt re-runs the auth/partner handlers and sends fresh headers.
/// </summary>
public sealed class PayPalRetryHandler : DelegatingHandler
{
    private const string IdempotencyHeader = "PayPal-Request-Id";

    private static readonly HashSet<HttpStatusCode> RetryableStatusCodes = new()
    {
        HttpStatusCode.RequestTimeout,       // 408
        (HttpStatusCode)429,                 // Too Many Requests
        HttpStatusCode.InternalServerError,  // 500
        HttpStatusCode.BadGateway,           // 502
        HttpStatusCode.ServiceUnavailable,   // 503
        HttpStatusCode.GatewayTimeout,       // 504
    };

    private readonly PayPalRetryOptions _options;

    public PayPalRetryHandler(IOptions<PayPalOptions> options) => _options = options.Value.Retry;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Not retryable at all, or retries disabled: send once, untouched.
        if (_options.MaxRetries <= 0 || !IsSafeToRetry(request))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Buffer the body once so every attempt sends identical bytes.
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var attemptRequest = await CloneAsync(request, body).ConfigureAwait(false);
            HttpResponseMessage? response = null;
            Exception? transientError = null;
            try
            {
                response = await base.SendAsync(attemptRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                transientError = ex; // connection failure, DNS, TLS, etc.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                transientError = null; // a request timeout (not the caller cancelling); treat as retryable below
            }

            // The caller cancelled: stop now, do not swallow it.
            cancellationToken.ThrowIfCancellationRequested();

            var isLastAttempt = attempt >= _options.MaxRetries;
            var isTransient = response is null || RetryableStatusCodes.Contains(response.StatusCode);

            if (!isTransient)
            {
                return response!; // permanent outcome (success or a 4xx): return as-is.
            }

            if (isLastAttempt)
            {
                if (response is not null)
                {
                    return response; // out of retries: hand back the last (transient) response.
                }
                // Out of retries after a network error/timeout: re-run once to surface the real exception.
                return await base.SendAsync(await CloneAsync(request, body).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            }

            var delay = ComputeDelay(attempt, response);
            response?.Dispose();
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    // Idempotent HTTP methods are always safe; POST/PATCH only when an idempotency key is present.
    private static bool IsSafeToRetry(HttpRequestMessage request)
    {
        var method = request.Method;
        if (method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options
            || method == HttpMethod.Delete || method == HttpMethod.Put)
        {
            return true;
        }
        return request.Headers.Contains(IdempotencyHeader);
    }

    private TimeSpan ComputeDelay(int attempt, HttpResponseMessage? response)
    {
        // Honor Retry-After (delta-seconds or HTTP-date) when the server sent one, capped by MaxDelay.
        if (response?.Headers.RetryAfter is { } retryAfter)
        {
            TimeSpan? server = retryAfter.Delta
                ?? (retryAfter.Date is { } date ? date - DateTimeOffset.UtcNow : null);
            if (server is { } s && s > TimeSpan.Zero)
            {
                return s < _options.MaxDelay ? s : _options.MaxDelay;
            }
        }

        // Exponential backoff with full jitter, capped at MaxDelay.
        var exponential = _options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt);
        var capped = Math.Min(exponential, _options.MaxDelay.TotalMilliseconds);
        var jittered = capped * (0.5 + (Random.Shared.NextDouble() * 0.5)); // 50%..100% of the cap
        return TimeSpan.FromMilliseconds(jittered);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, byte[]? body)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            ((IDictionary<string, object?>)clone.Options)[option.Key] = option.Value;
        }

        if (body is not null)
        {
            clone.Content = new ByteArrayContent(body);
            if (request.Content is not null)
            {
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        return await Task.FromResult(clone);
    }
}
