using System.Net;
using Aeroverra.PayPalSharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Proves auth is shared, not multiplied: the token provider is a singleton (so every injected client
/// and every DI scope shares one token cache), and concurrent demand collapses to a single token fetch.
/// A counting primary handler stands in for PayPal, so no network is used.
/// </summary>
public class TokenCachingTests
{
    private sealed class CountingHandler : HttpMessageHandler
    {
        public int TokenCalls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref TokenCalls);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"fake-token\",\"token_type\":\"Bearer\",\"expires_in\":32400}"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        }
    }

    [Fact]
    public void Token_provider_is_registered_as_a_singleton()
    {
        using var sp = new ServiceCollection()
            .AddPayPalSharp(o => { o.ClientId = "id"; o.ClientSecret = "secret"; })
            .BuildServiceProvider();

        // Same instance every resolution and in every scope => one shared token cache app-wide.
        var root = sp.GetRequiredService<IPayPalTokenProvider>();
        using var scope1 = sp.CreateScope();
        using var scope2 = sp.CreateScope();
        Assert.Same(root, scope1.ServiceProvider.GetRequiredService<IPayPalTokenProvider>());
        Assert.Same(root, scope2.ServiceProvider.GetRequiredService<IPayPalTokenProvider>());
    }

    [Fact]
    public async Task One_token_is_fetched_and_shared_under_concurrent_load()
    {
        var handler = new CountingHandler();
        var services = new ServiceCollection();
        services.AddPayPalSharp(o => { o.Environment = PayPalEnvironment.Sandbox; o.ClientId = "id"; o.ClientSecret = "secret"; });
        services.ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(() => handler));
        using var sp = services.BuildServiceProvider();

        var provider = sp.GetRequiredService<IPayPalTokenProvider>();

        // 100 concurrent demands for a token from the shared singleton.
        var tokens = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => provider.GetAccessTokenAsync()));

        Assert.All(tokens, t => Assert.Equal("fake-token", t));
        Assert.Equal(1, handler.TokenCalls); // cache + semaphore: one fetch serves everyone

        // A later demand still reuses the cached token (no second fetch).
        await provider.GetAccessTokenAsync();
        Assert.Equal(1, handler.TokenCalls);
    }
}
