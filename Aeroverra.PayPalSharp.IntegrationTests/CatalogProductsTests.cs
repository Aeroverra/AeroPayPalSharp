using Aeroverra.PayPalSharp.CatalogProductsV1;
using Xunit;
using Xunit.Abstractions;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>Live Catalog Products v1: create a product, read it back, and list.</summary>
[Collection(PayPalCollection.Name)]
public class CatalogProductsTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public CatalogProductsTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    [SkippableFact]
    public async Task Create_read_and_list_a_product()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        var product = new Product_request_POST
        {
            Name = "Aero Test Product " + Guid.NewGuid().ToString("N")[..8],
            Type = "SERVICE",
            Description = "Created by the AeroPayPalSharp integration tests.",
            Category = "SOFTWARE",
        };

        var created = await _fx.Client.CatalogProducts.CreateAsync(body: product);
        Assert.False(string.IsNullOrEmpty(created.Id));
        Assert.Equal(product.Name, created.Name);
        _output.WriteLine($"product id={created.Id}");

        var fetched = await _fx.Client.CatalogProducts.GetAsync(created.Id);
        Assert.Equal(created.Id, fetched.Id);

        var list = await _fx.Client.CatalogProducts.ListAsync();
        Assert.NotNull(list);
    }
}
