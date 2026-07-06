using System.Net;
using System.Text;
using Aeroverra.PayPalSharp;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Proves the multi-tenant factory is thread-safe and per-credential isolated (no token leaks across
/// merchants under concurrency), that clients are cached per credential set, and that the
/// bring-your-own-token entry points work. A fake transport mints a token derived from the client id
/// in the Basic auth header, so each credential set is distinguishable without network.
/// </summary>
public class FactoryMultiTenantTests
{
    // The factory's shared transport. Returns "tok-<clientId>" so we can tell whose token a client holds.
    private sealed class TokenPerClientHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal))
            {
                var basic = request.Headers.Authorization!.Parameter!;
                var clientId = Encoding.UTF8.GetString(Convert.FromBase64String(basic)).Split(':')[0];
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"access_token\":\"tok-{clientId}\",\"token_type\":\"Bearer\",\"expires_in\":32400}}"),
                });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        }
    }

    [Fact]
    public async Task Concurrent_clients_for_different_merchants_do_not_share_tokens()
    {
        using var factory = new PayPalClientFactory(new TokenPerClientHandler());

        // 5 merchants, hit from 500 concurrent threads, interleaved.
        var results = await Task.WhenAll(Enumerable.Range(0, 500).Select(i => Task.Run(async () =>
        {
            var id = "merchant-" + (i % 5);
            var client = factory.Create(id, "secret-" + (i % 5), PayPalEnvironment.Sandbox);
            var token = await client.Tokens.GetAccessTokenAsync();
            return (id, token);
        })));

        foreach (var (id, token) in results)
        {
            Assert.Equal($"tok-{id}", token); // each merchant's client holds only its own token
        }
    }

    [Fact]
    public void Same_credentials_return_the_same_cached_client()
    {
        using var factory = new PayPalClientFactory(new TokenPerClientHandler());
        var a = factory.Create("id", "secret", PayPalEnvironment.Sandbox);
        var b = factory.Create("id", "secret", PayPalEnvironment.Sandbox);
        var other = factory.Create("id2", "secret", PayPalEnvironment.Sandbox);

        Assert.Same(a, b);
        Assert.NotSame(a, other);
    }

    [Fact]
    public async Task CreateWithAccessToken_uses_the_supplied_token()
    {
        using var factory = new PayPalClientFactory(new TokenPerClientHandler());
        var client = factory.CreateWithAccessToken("byo-token-123", PayPalEnvironment.Sandbox);
        Assert.Equal("byo-token-123", await client.Tokens.GetAccessTokenAsync());
    }

    [Fact]
    public async Task CreateWithTokenProvider_calls_your_delegate_for_each_token()
    {
        using var factory = new PayPalClientFactory(new TokenPerClientHandler());
        var calls = 0;
        var client = factory.CreateWithTokenProvider(
            new DelegatePayPalTokenProvider(_ => Task.FromResult("dyn-" + Interlocked.Increment(ref calls))));

        Assert.Equal("dyn-1", await client.Tokens.GetAccessTokenAsync());
        Assert.Equal("dyn-2", await client.Tokens.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Static_token_provider_returns_the_token_and_rejects_empty()
    {
        Assert.Equal("t", await new StaticPayPalTokenProvider("t").GetAccessTokenAsync());
        Assert.Throws<ArgumentException>(() => new StaticPayPalTokenProvider(""));
    }
}
