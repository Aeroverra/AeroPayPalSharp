using Xunit;
using Xunit.Abstractions;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Verifies the runtime factory: building a working client from raw credentials (the
/// multi-tenant path, no DI-configured account) and per-credential caching.
/// </summary>
[Collection(PayPalCollection.Name)]
public class FactoryTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public FactoryTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    [SkippableFact]
    public async Task Factory_builds_a_working_client_from_raw_credentials()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        using var factory = new PayPalClientFactory();
        var client = factory.Create(
            _fx.Options.ClientId,
            _fx.Options.ClientSecret,
            _fx.Options.Environment,
            _fx.Options.PartnerAttributionId);

        var catalog = await client.Webhooks.WebhooksEventTypesListAsync();

        Assert.NotEmpty(catalog.EventTypes);
        _output.WriteLine($"factory-built client saw {catalog.EventTypes.Count} event types");
    }

    [SkippableFact]
    public void Factory_reuses_one_client_per_credential_set()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        using var factory = new PayPalClientFactory();

        var a = factory.Create(_fx.Options.ClientId, _fx.Options.ClientSecret, _fx.Options.Environment);
        var b = factory.Create(_fx.Options.ClientId, _fx.Options.ClientSecret, _fx.Options.Environment);
        Assert.Same(a, b); // same credentials, cached instance (and its token cache) reused

        var different = factory.Create("some-other-client-id", "some-other-secret", _fx.Options.Environment);
        Assert.NotSame(a, different);
    }
}
