using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// The innermost handler: invokes the optional <c>OnRequest</c>/<c>OnResponse</c> callbacks and, when
/// <see cref="PayPalLoggingOptions.Enabled"/> is set, logs a one-line summary per attempt (method, path,
/// status, elapsed, PayPal-Debug-Id). It sees the final request (auth + partner headers attached) and the
/// raw response, and runs once per retry attempt so retries are visible. Callback exceptions are swallowed
/// so a faulty hook can never break a payment call.
/// </summary>
public sealed class PayPalObservabilityHandler : DelegatingHandler
{
    private readonly PayPalOptions _options;
    private readonly ILogger<PayPalObservabilityHandler>? _logger;

    public PayPalObservabilityHandler(IOptions<PayPalOptions> options, ILogger<PayPalObservabilityHandler>? logger = null)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Invoke(_options.OnRequest, request);

        var logging = _options.Logging.Enabled && _logger is not null && _logger.IsEnabled(MapLevel(_options.Logging.Level));
        var stopwatch = logging ? Stopwatch.StartNew() : null;

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Transport failure (network/timeout/cancel): no response, so OnException fires (not OnResponse).
            Invoke(_options.OnException, request, ex);
            if (logging)
            {
                _logger!.Log(MapLevel(_options.Logging.Level), ex, "PayPal {Method} {Path} failed after {Elapsed}ms",
                    request.Method, request.RequestUri?.AbsolutePath, stopwatch!.ElapsedMilliseconds);
            }
            throw;
        }

        Invoke(_options.OnResponse, response);

        if (logging)
        {
            _logger!.Log(MapLevel(_options.Logging.Level),
                "PayPal {Method} {Path} -> {Status} in {Elapsed}ms (debug-id: {DebugId})",
                request.Method, request.RequestUri?.AbsolutePath, (int)response.StatusCode,
                stopwatch!.ElapsedMilliseconds, PayPalHeaders.GetDebugId(response) ?? "-");
        }

        return response;
    }

    private static void Invoke<T>(Action<T>? callback, T value)
    {
        if (callback is null)
        {
            return;
        }
        try
        {
            callback(value);
        }
        catch
        {
            // A callback must never break the request.
        }
    }

    private static void Invoke<T1, T2>(Action<T1, T2>? callback, T1 a, T2 b)
    {
        if (callback is null)
        {
            return;
        }
        try
        {
            callback(a, b);
        }
        catch
        {
            // A callback must never break the request.
        }
    }

    private static LogLevel MapLevel(LogLevelOption level) => level switch
    {
        LogLevelOption.Trace => LogLevel.Trace,
        LogLevelOption.Information => LogLevel.Information,
        LogLevelOption.Warning => LogLevel.Warning,
        _ => LogLevel.Debug,
    };
}
