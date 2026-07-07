using System.Net;
using Aeroverra.PayPalSharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Proves the transport hook (PrimaryHandlerFactory) and the client hook (ConfigureHttpClient) apply on
/// BOTH the DI and factory paths - the mechanism proxy support rides on. No network.
/// </summary>
public class HttpConfigTests
{
    // A primary handler we can detect was used, standing in for a proxy-configured handler. It also
    // serves a valid token for the oauth endpoint so the client-credentials flow can complete (this also
    // confirms the proxy/transport handler covers the token fetch, not just API calls).
    private sealed class MarkerHandler : HttpMessageHandler
    {
        public bool WasUsed { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasUsed = true;
            var body = request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal)
                ? "{\"access_token\":\"tok\",\"token_type\":\"Bearer\",\"expires_in\":32400}"
                : "{}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    [Fact]
    public async Task DI_path_uses_PrimaryHandlerFactory_and_ConfigureHttpClient()
    {
        var marker = new MarkerHandler();
        var headerSeen = false;

        using var sp = new ServiceCollection()
            .AddPayPalSharp(o =>
            {
                o.ClientId = "id"; o.ClientSecret = "secret";
                o.PrimaryHandlerFactory = () => marker;                       // e.g. a proxy handler
                o.ConfigureHttpClient = c => { headerSeen = true; c.Timeout = TimeSpan.FromSeconds(7); };
            })
            .BuildServiceProvider();

        var client = sp.GetRequiredService<IPayPalApiClient>();
        // Any call flows through the primary handler; a 404 lookup is enough to exercise it.
        try { await client.Orders.GetAsync("nope"); } catch (PayPalApiException) { }

        Assert.True(marker.WasUsed, "PrimaryHandlerFactory handler was not used on the DI path.");
        Assert.True(headerSeen, "ConfigureHttpClient was not invoked on the DI path.");
    }

    [Fact]
    public async Task Factory_path_uses_PrimaryHandlerFactory_and_ConfigureHttpClient()
    {
        var marker = new MarkerHandler();
        var configured = false;

        using var sp = new ServiceCollection()
            .AddPayPalSharp(o =>
            {
                o.ClientId = "id"; o.ClientSecret = "secret";
                o.PrimaryHandlerFactory = () => marker;
                o.ConfigureHttpClient = _ => configured = true;
            })
            .BuildServiceProvider();

        var factory = sp.GetRequiredService<IPayPalClientFactory>();
        var client = factory.CreateWithAccessToken("token", PayPalEnvironment.Sandbox);
        try { await client.Orders.GetAsync("nope"); } catch (PayPalApiException) { }

        Assert.True(marker.WasUsed, "PrimaryHandlerFactory handler was not used on the factory path.");
        Assert.True(configured, "ConfigureHttpClient was not invoked on the factory path.");
    }

    [Fact]
    public void Default_factory_still_works_without_hooks()
    {
        using var factory = new PayPalClientFactory();
        var client = factory.CreateWithAccessToken("token", PayPalEnvironment.Sandbox);
        Assert.NotNull(client);
    }
}
