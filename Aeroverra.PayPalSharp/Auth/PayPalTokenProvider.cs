using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>Supplies (and caches) PayPal OAuth2 access tokens.</summary>
public interface IPayPalTokenProvider
{
    /// <summary>Returns a valid bearer access token, fetching/refreshing as needed.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Client-credentials token provider. POSTs to <c>/v1/oauth2/token</c> with HTTP Basic
/// (client id:secret), caches the token, and refreshes ~1 minute before expiry. A
/// semaphore collapses concurrent refreshes into one request.
/// </summary>
public sealed class PayPalTokenProvider : IPayPalTokenProvider
{
    private readonly Func<HttpClient> _httpClientAccessor;
    private readonly PayPalOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    /// <summary>Uses a fixed <see cref="HttpClient"/> (the factory path builds one over its shared transport).</summary>
    public PayPalTokenProvider(HttpClient httpClient, IOptions<PayPalOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClientAccessor = () => httpClient;
        _options = options.Value;
    }

    /// <summary>
    /// Resolves an <see cref="HttpClient"/> from the factory per fetch (the DI path). Registered as a
    /// singleton so one token is cached and shared across every client, instead of one cache per client.
    /// </summary>
    public PayPalTokenProvider(IHttpClientFactory httpClientFactory, string httpClientName, IOptions<PayPalOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClientAccessor = () => httpClientFactory.CreateClient(httpClientName);
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: a still-valid cached token.
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
        {
            return _cachedToken;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Re-check inside the lock - another caller may have just refreshed.
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
            {
                return _cachedToken;
            }

            if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                throw new InvalidOperationException(
                    "PayPal ClientId/ClientSecret are not configured. Set them via AddPayPalSharp or configuration (user-secrets).");
            }

            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v1/oauth2/token")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", basic) },
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                }),
            };

            using var response = await _httpClientAccessor().SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new PayPalAuthenticationException(
                    $"PayPal token request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(body, 500)}");
            }

            var token = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(body);
            if (token is null || string.IsNullOrEmpty(token.AccessToken))
            {
                throw new PayPalAuthenticationException("PayPal token response did not contain an access_token.");
            }

            _cachedToken = token.AccessToken;
            // Refresh a minute early; guard against absurdly small expiry values.
            var lifetime = Math.Max(token.ExpiresIn - 60, 30);
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(lifetime);
            return _cachedToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max] + "...";

    private sealed class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}

/// <summary>Thrown when PayPal authentication (token acquisition) fails.</summary>
public sealed class PayPalAuthenticationException : Exception
{
    public PayPalAuthenticationException(string message) : base(message) { }
}
