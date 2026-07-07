using System.Text.Json;
using System.Text.Json.Nodes;
using Aeroverra.PayPalSharp;
using Aeroverra.PayPalSharp.OrdersV2;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// The payment-source methods PayPal recognizes but omits from its request spec are now typed and
/// serialize correctly (pay_upon_invoice fully typed; the simple APMs via a permissive shape with an
/// extension-data escape hatch). No network.
/// </summary>
public class PaymentSourceTests
{
    private static readonly JsonSerializerOptions Options = Build();

    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions();
        PayPalJsonSettings.Apply(options);
        return options;
    }

    [Fact]
    public void Pay_upon_invoice_serializes_with_its_typed_fields()
    {
        var order = new OrderRequest
        {
            Intent = PayPalIntent.Capture,
            PaymentSource = new PaymentSource
            {
                PayUponInvoice = new PayUponInvoiceRequest
                {
                    Email = "buyer@example.de",
                    BirthDate = "1990-01-01",
                    Name = new Name { GivenName = "John", Surname = "Doe" },
                    BillingAddress = new AddressPortable { CountryCode = "DE", PostalCode = "13353" },
                },
            },
        };

        var pui = JsonNode.Parse(JsonSerializer.Serialize(order, Options))!["payment_source"]!["pay_upon_invoice"]!;
        Assert.Equal("buyer@example.de", pui["email"]!.GetValue<string>());
        Assert.Equal("1990-01-01", pui["birth_date"]!.GetValue<string>());
        Assert.Equal("John", pui["name"]!["given_name"]!.GetValue<string>());
        Assert.Equal("DE", pui["billing_address"]!["country_code"]!.GetValue<string>());
    }

    [Fact]
    public void A_simple_apm_serializes_and_supports_extra_fields()
    {
        var apm = new ApmRequest { CountryCode = "PT", Name = new Name { GivenName = "Ana" } };
        apm.AdditionalProperties["bic"] = "ABCDPTPL"; // method-specific field via the extension-data hatch

        var order = new OrderRequest
        {
            Intent = PayPalIntent.Capture,
            PaymentSource = new PaymentSource { Multibanco = apm },
        };

        var mb = JsonNode.Parse(JsonSerializer.Serialize(order, Options))!["payment_source"]!["multibanco"]!;
        Assert.Equal("PT", mb["country_code"]!.GetValue<string>());
        Assert.Equal("Ana", mb["name"]!["given_name"]!.GetValue<string>());
        Assert.Equal("ABCDPTPL", mb["bic"]!.GetValue<string>()); // flowed through additionalProperties
    }
}
