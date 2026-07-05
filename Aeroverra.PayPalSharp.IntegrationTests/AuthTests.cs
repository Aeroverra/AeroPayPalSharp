using Xunit;
using Xunit.Abstractions;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>OAuth2 client-credentials token acquisition + caching against the sandbox.</summary>
[Collection(PayPalCollection.Name)]
public class AuthTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public AuthTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    [SkippableFact]
    public async Task Token_is_issued()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        var token = await _fx.TokenProvider.GetAccessTokenAsync();

        Assert.False(string.IsNullOrWhiteSpace(token));
        _output.WriteLine($"access token length: {token.Length}");
    }

    [SkippableFact]
    public async Task Token_is_cached_between_calls()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        var first = await _fx.TokenProvider.GetAccessTokenAsync();
        var second = await _fx.TokenProvider.GetAccessTokenAsync();

        // Same token instance is returned until it nears expiry - no second round-trip.
        Assert.Equal(first, second);
    }
}
