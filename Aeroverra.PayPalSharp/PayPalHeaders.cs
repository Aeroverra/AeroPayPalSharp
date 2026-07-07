using System.Net.Http.Headers;

namespace Aeroverra.PayPalSharp;

/// <summary>Helpers for the PayPal-specific response headers you occasionally need to read.</summary>
public static class PayPalHeaders
{
    /// <summary>The header PayPal returns on every response; quote it to PayPal support to trace a call.</summary>
    public const string DebugIdHeader = "PayPal-Debug-Id";

    /// <summary>
    /// Returns the <c>PayPal-Debug-Id</c> from a response (null if absent). Available on success via the
    /// <see cref="PayPalOptions.OnResponse"/> callback, and on failure via
    /// <c>PayPalApiException.Headers</c>.
    /// </summary>
    public static string? GetDebugId(HttpResponseMessage response)
        => response is null ? null : GetDebugId(response.Headers);

    /// <inheritdoc cref="GetDebugId(HttpResponseMessage)"/>
    public static string? GetDebugId(HttpResponseHeaders headers)
        => headers is not null && headers.TryGetValues(DebugIdHeader, out var values)
            ? values.FirstOrDefault()
            : null;

    /// <summary>
    /// Returns the <c>PayPal-Debug-Id</c> from a generated client's exception headers (null if absent).
    /// </summary>
    public static string? GetDebugId(IReadOnlyDictionary<string, IEnumerable<string>>? headers)
        => headers is not null && headers.TryGetValue(DebugIdHeader, out var values)
            ? values.FirstOrDefault()
            : null;
}
