namespace Aeroverra.PayPalSharp;

/// <summary>
/// An <see cref="IPayPalTokenProvider"/> that returns a fixed access token you already hold. It never
/// refreshes, so once the token expires calls will start returning 401. Use it when another system owns
/// token acquisition and hands you a token; if the token rotates, either build a new client with the new
/// token or use <see cref="DelegatePayPalTokenProvider"/> so one client can keep fetching fresh tokens.
/// </summary>
public sealed class StaticPayPalTokenProvider : IPayPalTokenProvider
{
    private readonly string _accessToken;

    public StaticPayPalTokenProvider(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("accessToken is required.", nameof(accessToken));
        }
        _accessToken = accessToken;
    }

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult(_accessToken);
}

/// <summary>
/// An <see cref="IPayPalTokenProvider"/> backed by your own callback. Bring your own token source: for
/// example a central token service, a cache, or a non client-credentials OAuth flow. Your callback is
/// responsible for returning a currently-valid token (and refreshing as needed); the SDK just attaches
/// whatever you return as the bearer.
/// </summary>
public sealed class DelegatePayPalTokenProvider : IPayPalTokenProvider
{
    private readonly Func<CancellationToken, Task<string>> _getAccessToken;

    public DelegatePayPalTokenProvider(Func<CancellationToken, Task<string>> getAccessToken)
        => _getAccessToken = getAccessToken ?? throw new ArgumentNullException(nameof(getAccessToken));

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => _getAccessToken(cancellationToken);
}
