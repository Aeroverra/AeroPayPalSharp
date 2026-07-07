namespace Aeroverra.PayPalSharp;

/// <summary>
/// Holds the sandbox <c>PayPal-Mock-Response</c> value for the current async flow. Set it with a scope
/// (<c>using (client.WithMockResponse("INSTRUMENT_DECLINED")) { ... }</c>); the mock-response handler
/// attaches the header to every call made inside the scope, but ONLY in the sandbox environment, so it can
/// never affect live traffic. Uses <see cref="System.Threading.AsyncLocal{T}"/> like
/// <see cref="PayPalMerchantContext"/>, so nested scopes and concurrent flows do not interfere.
/// </summary>
public sealed class PayPalMockResponseContext
{
    private readonly System.Threading.AsyncLocal<string?> _current = new();

    /// <summary>The full <c>PayPal-Mock-Response</c> header value in effect for this flow, or null.</summary>
    public string? CurrentHeaderValue => _current.Value;

    /// <summary>Begins a scope whose calls carry the given header value. Dispose to end it.</summary>
    public IDisposable Begin(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            throw new ArgumentException("headerValue is required.", nameof(headerValue));
        }

        var previous = _current.Value;
        _current.Value = headerValue;
        return new Scope(this, previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly PayPalMockResponseContext _context;
        private readonly string? _previous;
        private bool _disposed;

        public Scope(PayPalMockResponseContext context, string? previous)
        {
            _context = context;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _context._current.Value = _previous;
        }
    }
}
