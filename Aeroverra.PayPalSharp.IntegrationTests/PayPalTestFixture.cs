using Aeroverra.PayPalSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Shared DI container for the live sandbox tests. Configures the SDK exactly as a
/// consumer would (<c>AddPayPalSharp(configuration)</c>). All credentials come from
/// user-secrets; when they're absent, <see cref="IsConfigured"/> is false and tests
/// skip rather than fail.
/// </summary>
public sealed class PayPalTestFixture
{
    private readonly ServiceProvider _provider;

    public IConfiguration Configuration { get; }
    public PayPalOptions Options { get; }
    public bool IsConfigured { get; }

    public string SkipReason =>
        "PayPal sandbox credentials not configured. Set user-secrets PayPal:ClientId and PayPal:ClientSecret (see README).";

    public PayPalTestFixture()
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<PayPalTestFixture>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddPayPalSharp(Configuration);
        _provider = services.BuildServiceProvider();

        Options = _provider.GetRequiredService<IOptions<PayPalOptions>>().Value;
        IsConfigured = !string.IsNullOrWhiteSpace(Options.ClientId) && !string.IsNullOrWhiteSpace(Options.ClientSecret);
    }

    public IPayPalApiClient Client => _provider.GetRequiredService<IPayPalApiClient>();
    public IPayPalTokenProvider TokenProvider => _provider.GetRequiredService<IPayPalTokenProvider>();

    /// <summary>A value present in user-secrets/appsettings, or null.</summary>
    public string? Data(string key) => Configuration[$"PayPal:{key}"];
}

[CollectionDefinition(Name)]
public sealed class PayPalCollection : ICollectionFixture<PayPalTestFixture>
{
    public const string Name = "PayPal Sandbox";
}
