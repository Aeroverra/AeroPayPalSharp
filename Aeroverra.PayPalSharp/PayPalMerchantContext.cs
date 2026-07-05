namespace Aeroverra.PayPalSharp;

/// <summary>
/// Holds the "act on behalf of this sub-merchant" value for the current async flow. Set it
/// with a scope (<c>using (paypal.ActingAsMerchant(id)) { ... }</c>); the partner header
/// handler reads it and attaches a <c>PayPal-Auth-Assertion</c> for that merchant to every
/// call made inside the scope. The value flows across awaits and is restored on dispose, so
/// nested scopes and concurrent flows do not interfere.
/// </summary>
public sealed class PayPalMerchantContext
{
    private readonly AsyncLocal<string?> _current = new();

    /// <summary>The sub-merchant id currently in effect, or null.</summary>
    public string? CurrentMerchantId => _current.Value;

    /// <summary>
    /// Begins a scope in which calls act on behalf of <paramref name="merchantId"/>. Dispose the
    /// returned handle (a <c>using</c> block) to end it and restore the previous value.
    /// </summary>
    public IDisposable ActingAs(string merchantId)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            throw new ArgumentException("merchantId is required.", nameof(merchantId));
        }

        var previous = _current.Value;
        _current.Value = merchantId;
        return new Scope(this, previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly PayPalMerchantContext _context;
        private readonly string? _previous;
        private bool _disposed;

        public Scope(PayPalMerchantContext context, string? previous)
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
