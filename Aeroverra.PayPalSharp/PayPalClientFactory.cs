using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Aeroverra.PayPalSharp.CatalogProductsV1;
using Aeroverra.PayPalSharp.CustomV1;
using Aeroverra.PayPalSharp.DisputesV1;
using Aeroverra.PayPalSharp.InvoicesV2;
using Aeroverra.PayPalSharp.OrdersV2;
using Aeroverra.PayPalSharp.PartnerReferralsV1;
using Aeroverra.PayPalSharp.PartnerReferralsV2;
using Aeroverra.PayPalSharp.PaymentsV2;
using Aeroverra.PayPalSharp.PaymentTokensV3;
using Aeroverra.PayPalSharp.PayoutsV1;
using Aeroverra.PayPalSharp.ShipmentTrackingV1;
using Aeroverra.PayPalSharp.SubscriptionsV1;
using Aeroverra.PayPalSharp.TransactionSearchV1;
using Aeroverra.PayPalSharp.WebhooksV1;
using Aeroverra.PayPalSharp.WebProfilesV1;
using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// The credentials for one PayPal account. Used with <see cref="IPayPalClientFactory"/>
/// to build a client for an arbitrary account at runtime (for example a service that
/// processes payments on behalf of many merchants, each with their own API keys).
/// </summary>
public sealed record PayPalCredentials
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public PayPalEnvironment Environment { get; init; } = PayPalEnvironment.Sandbox;
    public string? PartnerAttributionId { get; init; }
    public string? MerchantId { get; init; }
    public bool SendAuthAssertion { get; init; }
    public string? BaseUrlOverride { get; init; }
    public int TimeoutSeconds { get; init; } = 100;

    internal PayPalOptions ToOptions() => new()
    {
        Environment = Environment,
        ClientId = ClientId,
        ClientSecret = ClientSecret,
        PartnerAttributionId = PartnerAttributionId,
        MerchantId = MerchantId,
        SendAuthAssertion = SendAuthAssertion,
        BaseUrlOverride = BaseUrlOverride,
        TimeoutSeconds = TimeoutSeconds,
    };

    // Identifies this exact credential set so the factory can reuse a built client
    // (and its token cache). The secret is hashed so it is not held as a dictionary key.
    internal string CacheKey =>
        $"{Environment}|{ClientId}|{PartnerAttributionId}|{MerchantId}|{SendAuthAssertion}|{BaseUrlOverride}|{TimeoutSeconds}|{HashSecret(ClientSecret)}";

    private static string HashSecret(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}

/// <summary>
/// Builds a <see cref="IPayPalApiClient"/> for any set of PayPal credentials at runtime,
/// without those credentials being registered in DI. Ideal for multi-tenant / non-partner
/// scenarios where you hold each merchant's own client id and secret.
/// </summary>
public interface IPayPalClientFactory
{
    /// <summary>Gets (or builds and caches) a client for the given credentials.</summary>
    IPayPalApiClient Create(PayPalCredentials credentials);

    /// <summary>Convenience overload for the common case.</summary>
    IPayPalApiClient Create(
        string clientId,
        string clientSecret,
        PayPalEnvironment environment = PayPalEnvironment.Sandbox,
        string? partnerAttributionId = null,
        string? merchantId = null);

    /// <summary>
    /// Builds a client that authenticates with an access token you already hold, instead of a client
    /// id/secret. The SDK does not fetch or refresh the token; when it expires, build a new client (or
    /// use <see cref="CreateWithTokenProvider"/> with a refreshing provider). Reuse the returned client
    /// rather than calling this per request. Pass <paramref name="partnerClientId"/> only if you need
    /// <c>ActingAsMerchant</c> to build an auth assertion (its issuer).
    /// </summary>
    IPayPalApiClient CreateWithAccessToken(
        string accessToken,
        PayPalEnvironment environment = PayPalEnvironment.Sandbox,
        string? partnerAttributionId = null,
        string? merchantId = null,
        string? partnerClientId = null,
        string? baseUrlOverride = null,
        int timeoutSeconds = 100);

    /// <summary>
    /// Builds a client whose bearer token comes from your own <see cref="IPayPalTokenProvider"/> (for
    /// example <see cref="DelegatePayPalTokenProvider"/> wrapping a central token service). Reuse the
    /// returned client. Pass <paramref name="partnerClientId"/> only if <c>ActingAsMerchant</c> needs it.
    /// </summary>
    IPayPalApiClient CreateWithTokenProvider(
        IPayPalTokenProvider tokenProvider,
        PayPalEnvironment environment = PayPalEnvironment.Sandbox,
        string? partnerAttributionId = null,
        string? merchantId = null,
        string? partnerClientId = null,
        string? baseUrlOverride = null,
        int timeoutSeconds = 100);
}

/// <summary>
/// Default factory. Reuses one shared transport handler across every built client (so it
/// does not exhaust sockets) and caches one client per distinct credential set (so tokens
/// are cached per account across calls). Thread-safe. Register it via
/// <c>AddPayPalSharp(...)</c> or <c>AddPayPalSharpFactory()</c>, or just <c>new PayPalClientFactory()</c>.
/// </summary>
public sealed class PayPalClientFactory : IPayPalClientFactory, IDisposable
{
    private readonly HttpMessageHandler _rootHandler;
    private readonly bool _ownsRootHandler;
    private readonly ConcurrentDictionary<string, Lazy<IPayPalApiClient>> _clients = new();
    private readonly List<HttpClient> _built = new();
    private readonly object _sync = new();
    private bool _disposed;

    /// <summary>Creates a factory with its own pooled transport handler.</summary>
    public PayPalClientFactory()
        : this(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }, ownsRootHandler: true)
    {
    }

    /// <summary>Creates a factory over a transport handler you own (it will not be disposed by the factory).</summary>
    public PayPalClientFactory(HttpMessageHandler rootHandler)
        : this(rootHandler, ownsRootHandler: false)
    {
    }

    private PayPalClientFactory(HttpMessageHandler rootHandler, bool ownsRootHandler)
    {
        _rootHandler = rootHandler;
        _ownsRootHandler = ownsRootHandler;
    }

    public IPayPalApiClient Create(PayPalCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (string.IsNullOrWhiteSpace(credentials.ClientId) || string.IsNullOrWhiteSpace(credentials.ClientSecret))
        {
            throw new ArgumentException("ClientId and ClientSecret are required.", nameof(credentials));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);
        return _clients.GetOrAdd(credentials.CacheKey, _ => new Lazy<IPayPalApiClient>(() => Build(credentials))).Value;
    }

    public IPayPalApiClient Create(
        string clientId,
        string clientSecret,
        PayPalEnvironment environment = PayPalEnvironment.Sandbox,
        string? partnerAttributionId = null,
        string? merchantId = null)
        => Create(new PayPalCredentials
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Environment = environment,
            PartnerAttributionId = partnerAttributionId,
            MerchantId = merchantId,
        });

    public IPayPalApiClient CreateWithAccessToken(
        string accessToken,
        PayPalEnvironment environment = PayPalEnvironment.Sandbox,
        string? partnerAttributionId = null,
        string? merchantId = null,
        string? partnerClientId = null,
        string? baseUrlOverride = null,
        int timeoutSeconds = 100)
        => CreateWithTokenProvider(
            new StaticPayPalTokenProvider(accessToken),
            environment, partnerAttributionId, merchantId, partnerClientId, baseUrlOverride, timeoutSeconds);

    public IPayPalApiClient CreateWithTokenProvider(
        IPayPalTokenProvider tokenProvider,
        PayPalEnvironment environment = PayPalEnvironment.Sandbox,
        string? partnerAttributionId = null,
        string? merchantId = null,
        string? partnerClientId = null,
        string? baseUrlOverride = null,
        int timeoutSeconds = 100)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var options = Microsoft.Extensions.Options.Options.Create(new PayPalOptions
        {
            Environment = environment,
            ClientId = partnerClientId ?? string.Empty,   // only used as the auth-assertion issuer
            PartnerAttributionId = partnerAttributionId,
            MerchantId = merchantId,
            SendAuthAssertion = false,
            BaseUrlOverride = baseUrlOverride,
            TimeoutSeconds = timeoutSeconds,
        });
        return BuildClient(options, tokenProvider);
    }

    private IPayPalApiClient Build(PayPalCredentials credentials)
    {
        var options = Microsoft.Extensions.Options.Options.Create(credentials.ToOptions());
        var baseUri = ResolveBaseUri(options.Value);

        // Token provider gets its own HttpClient over the shared transport (no auth handler).
        var tokenHttp = new HttpClient(new NonDisposingHandler(_rootHandler), disposeHandler: true)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds),
        };
        lock (_sync)
        {
            _built.Add(tokenHttp);
        }

        return BuildClient(options, new PayPalTokenProvider(tokenHttp, options));
    }

    private IPayPalApiClient BuildClient(IOptions<PayPalOptions> options, IPayPalTokenProvider tokenProvider)
    {
        var baseUri = ResolveBaseUri(options.Value);

        // API HttpClient: partner headers -> auth (bearer) -> shared transport.
        var merchantContext = new PayPalMerchantContext();
        var partner = new PayPalPartnerHeaderHandler(options, merchantContext) { InnerHandler = new NonDisposingHandler(_rootHandler) };
        var auth = new PayPalAuthenticationHandler(tokenProvider) { InnerHandler = partner };
        var apiHttp = new HttpClient(auth, disposeHandler: true)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds),
        };
        apiHttp.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        apiHttp.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en_US");

        lock (_sync)
        {
            _built.Add(apiHttp);
        }

        // All generated clients share one configured HttpClient.
        return new PayPalApiClient(
            new OrdersV2Client(apiHttp),
            new PaymentsV2Client(apiHttp),
            new InvoicesV2Client(apiHttp),
            new SubscriptionsV1Client(apiHttp),
            new CatalogProductsV1Client(apiHttp),
            new DisputesV1Client(apiHttp),
            new PayoutsV1Client(apiHttp),
            new TransactionSearchV1Client(apiHttp),
            new ShipmentTrackingV1Client(apiHttp),
            new PaymentTokensV3Client(apiHttp),
            new WebProfilesV1Client(apiHttp),
            new PartnerReferralsV2Client(apiHttp),
            new PartnerReferralsV1Client(apiHttp),
            new WebhooksV1Client(apiHttp),
            new PayPalCustomClient(apiHttp),
            tokenProvider,
            merchantContext);
    }

    private static Uri ResolveBaseUri(PayPalOptions options)
    {
        var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
        return new Uri(baseUrl);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        lock (_sync)
        {
            foreach (var client in _built)
            {
                client.Dispose();
            }
            _built.Clear();
        }

        if (_ownsRootHandler)
        {
            _rootHandler.Dispose();
        }
    }

    // Wraps the shared transport so an individual HttpClient's disposal does not tear down
    // the handler that every other built client is still using.
    private sealed class NonDisposingHandler : DelegatingHandler
    {
        public NonDisposingHandler(HttpMessageHandler inner) : base(inner) { }

        protected override void Dispose(bool disposing)
        {
            // Intentionally do not dispose the shared inner handler.
        }
    }
}
