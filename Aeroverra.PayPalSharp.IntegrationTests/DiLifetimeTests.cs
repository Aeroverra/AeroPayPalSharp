using Aeroverra.PayPalSharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// The injected IPayPalApiClient is a singleton, so it can be consumed by services of any lifetime -
/// including singletons - and passes the container's scope validation. No network.
/// </summary>
public class DiLifetimeTests
{
    // A singleton that depends on the client - the exact shape that failed before (a singleton consuming
    // a scoped client).
    private sealed class SingletonConsumer(IPayPalApiClient paypal)
    {
        public IPayPalApiClient Client { get; } = paypal;
    }

    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddPayPalSharp(o => { o.ClientId = "id"; o.ClientSecret = "secret"; });
        services.AddSingleton<SingletonConsumer>();
        // ValidateScopes + ValidateOnBuild is what surfaced the captive-dependency error.
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public void A_singleton_can_consume_the_client()
    {
        using var sp = Build();
        var consumer = sp.GetRequiredService<SingletonConsumer>(); // would throw if the client were scoped
        Assert.NotNull(consumer.Client);
    }

    [Fact]
    public void The_client_is_a_singleton_instance()
    {
        using var sp = Build();
        var a = sp.GetRequiredService<IPayPalApiClient>();
        var b = sp.GetRequiredService<IPayPalApiClient>();
        Assert.Same(a, b);
    }
}
