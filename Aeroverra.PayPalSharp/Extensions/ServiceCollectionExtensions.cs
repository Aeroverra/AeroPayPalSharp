using Aeroverra.PayPalSharp.PartnerReferralsV1;
using Aeroverra.PayPalSharp.PartnerReferralsV2;
using Aeroverra.PayPalSharp.WebhooksV1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>Registers the PayPal clients in the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the PayPal SDK. Inject <see cref="IPayPalApiClient"/> (or any individual
    /// sub-client interface) afterwards. Auth (OAuth2 client-credentials) and partner
    /// headers are wired automatically from <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddPayPalSharp(this IServiceCollection services, Action<PayPalOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);

        // Token provider gets its own HttpClient (no auth handler - it *is* the auth).
        services.AddHttpClient<IPayPalTokenProvider, PayPalTokenProvider>(ConfigureHttpClient);

        services.AddTransient<PayPalAuthenticationHandler>();
        services.AddTransient<PayPalPartnerHeaderHandler>();

        AddApiClient<IPartnerReferralsV2Client, PartnerReferralsV2Client>(services);
        AddApiClient<IPartnerReferralsV1Client, PartnerReferralsV1Client>(services);
        AddApiClient<IWebhooksV1Client, WebhooksV1Client>(services);

        services.AddScoped<IPayPalApiClient, PayPalApiClient>();
        services.AddPayPalSharpFactory();
        return services;
    }

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

    /// <summary>Adds the PayPal SDK, binding options from the "PayPal" configuration section.</summary>
    public static IServiceCollection AddPayPalSharp(this IServiceCollection services, IConfiguration configuration)
        => services.AddPayPalSharp(options => configuration.GetSection(PayPalOptions.SectionName).Bind(options));

    private static void AddApiClient<TInterface, TImplementation>(IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddHttpClient<TInterface, TImplementation>(ConfigureHttpClient)
            .AddHttpMessageHandler<PayPalAuthenticationHandler>()
            .AddHttpMessageHandler<PayPalPartnerHeaderHandler>();
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
