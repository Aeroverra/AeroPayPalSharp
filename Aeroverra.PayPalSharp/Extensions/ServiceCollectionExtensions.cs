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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>Registers the PayPal clients in the DI container.</summary>
public static class ServiceCollectionExtensions
{
    internal const string TokenHttpClientName = "Aeroverra.PayPalSharp.Token";

    /// <summary>
    /// Adds the PayPal SDK. Inject <see cref="IPayPalApiClient"/> (or any individual
    /// sub-client interface) afterwards. Auth (OAuth2 client-credentials) and partner
    /// headers are wired automatically from <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddPayPalSharp(this IServiceCollection services, Action<PayPalOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        services.TryAddSingleton<PayPalMerchantContext>();

        // Offline webhook signature verification (cert fetch + RSA verify, no API round-trip).
        services.TryAddSingleton<IPayPalCertificateSource, HttpPayPalCertificateSource>();
        services.TryAddSingleton<IPayPalWebhookVerifier, PayPalWebhookVerifier>();

        services.AddTransient<PayPalRetryHandler>();
        services.AddTransient<PayPalObservabilityHandler>();

        // Token provider gets its own named HttpClient (no auth handler - it *is* the auth). It is a
        // SINGLETON so one OAuth token is cached and shared across every client and request, rather than
        // one cache per injected client (the typed-client pattern would make it transient). The token POST
        // is safe to retry (a duplicate just returns another valid token), so it gets the retry handler too.
        services.AddHttpClient(TokenHttpClientName, ConfigureHttpClient)
            .AddHttpMessageHandler<PayPalRetryHandler>()
            .AddHttpMessageHandler<PayPalObservabilityHandler>();
        services.TryAddSingleton<IPayPalTokenProvider>(sp => new PayPalTokenProvider(
            sp.GetRequiredService<IHttpClientFactory>(),
            TokenHttpClientName,
            sp.GetRequiredService<IOptions<PayPalOptions>>()));

        services.AddTransient<PayPalAuthenticationHandler>();
        services.AddTransient<PayPalPartnerHeaderHandler>();

        AddApiClient<IOrdersV2Client, OrdersV2Client>(services);
        AddApiClient<IPaymentsV2Client, PaymentsV2Client>(services);
        AddApiClient<IInvoicesV2Client, InvoicesV2Client>(services);
        AddApiClient<ISubscriptionsV1Client, SubscriptionsV1Client>(services);
        AddApiClient<ICatalogProductsV1Client, CatalogProductsV1Client>(services);
        AddApiClient<IDisputesV1Client, DisputesV1Client>(services);
        AddApiClient<IPayoutsV1Client, PayoutsV1Client>(services);
        AddApiClient<ITransactionSearchV1Client, TransactionSearchV1Client>(services);
        AddApiClient<IShipmentTrackingV1Client, ShipmentTrackingV1Client>(services);
        AddApiClient<IPaymentTokensV3Client, PaymentTokensV3Client>(services);
        AddApiClient<IWebProfilesV1Client, WebProfilesV1Client>(services);
        AddApiClient<IPartnerReferralsV2Client, PartnerReferralsV2Client>(services);
        AddApiClient<IPartnerReferralsV1Client, PartnerReferralsV1Client>(services);
        AddApiClient<IWebhooksV1Client, WebhooksV1Client>(services);
        AddApiClient<IPayPalCustomClient, PayPalCustomClient>(services);

        services.AddScoped<IPayPalApiClient, PayPalApiClient>();
        services.AddPayPalSharpFactory();
        return services;
    }

    /// <summary>Adds the PayPal SDK, binding options from the "PayPal" configuration section.</summary>
    public static IServiceCollection AddPayPalSharp(this IServiceCollection services, IConfiguration configuration)
        => services.AddPayPalSharp(options => configuration.GetSection(PayPalOptions.SectionName).Bind(options));

    /// <summary>
    /// Registers just the runtime factory (<see cref="IPayPalClientFactory"/>) for building a
    /// client from any account's credentials at call time. Use this alone when you do NOT have a
    /// single set of globally-configured credentials (for example a service that handles payments
    /// for many merchants using their own raw API keys). Safe to call alongside AddPayPalSharp.
    /// </summary>
    public static IServiceCollection AddPayPalSharpFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<IPayPalClientFactory, PayPalClientFactory>();
        return services;
    }

    private static void AddApiClient<TInterface, TImplementation>(IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        // Handler order (outermost -> innermost): retry wraps everything so each attempt re-runs auth +
        // partner and sends fresh headers; observability is innermost so it logs the final request and
        // each attempt.
        services.AddHttpClient<TInterface, TImplementation>(ConfigureHttpClient)
            .AddHttpMessageHandler<PayPalRetryHandler>()
            .AddHttpMessageHandler<PayPalAuthenticationHandler>()
            .AddHttpMessageHandler<PayPalPartnerHeaderHandler>()
            .AddHttpMessageHandler<PayPalObservabilityHandler>();
    }

    private static void ConfigureHttpClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<PayPalOptions>>().Value;
        // Trailing slash is required so the generated clients' relative paths resolve.
        var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en_US");
    }
}
