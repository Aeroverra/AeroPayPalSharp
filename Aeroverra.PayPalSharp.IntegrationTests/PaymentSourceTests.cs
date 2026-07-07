using Aeroverra.PayPalSharp;
using Aeroverra.PayPalSharp.OrdersV2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// The payment-source methods PayPal recognizes but omits from its request spec are now typed and
/// serialize correctly (pay_upon_invoice fully typed; the simple APMs via a permissive shape with an
/// extension-data escape hatch). No network.
/// </summary>
public class PaymentSourceTests
{
    private static readonly JsonSerializerSettings Settings = Build();

    private static JsonSerializerSettings Build()
    {
        var s = new JsonSerializerSettings();
        PayPalJsonSettings.Apply(s);
        return s;
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

        var json = JObject.Parse(JsonConvert.SerializeObject(order, Settings));
        var pui = json["payment_source"]!["pay_upon_invoice"]!;
        Assert.Equal("buyer@example.de", (string)pui["email"]!);
        Assert.Equal("1990-01-01", (string)pui["birth_date"]!);
        Assert.Equal("John", (string)pui["name"]!["given_name"]!);
        Assert.Equal("DE", (string)pui["billing_address"]!["country_code"]!);
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

        var json = JObject.Parse(JsonConvert.SerializeObject(order, Settings));
        var mb = json["payment_source"]!["multibanco"]!;
        Assert.Equal("PT", (string)mb["country_code"]!);
        Assert.Equal("Ana", (string)mb["name"]!["given_name"]!);
        Assert.Equal("ABCDPTPL", (string)mb["bic"]!); // flowed through additionalProperties
    }
}
