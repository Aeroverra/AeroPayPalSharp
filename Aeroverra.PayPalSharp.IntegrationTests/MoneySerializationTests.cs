using Aeroverra.PayPalSharp;
using Aeroverra.PayPalSharp.OrdersV2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Money is a C# <see cref="decimal"/> but must serialize as a JSON string that never exceeds the
/// currency's decimal places (JPY 0, USD 2). No network.
/// </summary>
public class MoneySerializationTests
{
    private static readonly JsonSerializerSettings Settings = BuildSettings();

    private static JsonSerializerSettings BuildSettings()
    {
        var settings = new JsonSerializerSettings();
        PayPalJsonSettings.Apply(settings);
        return settings;
    }

    private static string ValueOf(AmountWithBreakdown amount) => (string)JObject.Parse(JsonConvert.SerializeObject(amount, Settings))["value"]!;

    [Theory]
    [InlineData("10.00", "10")]      // whole USD amount -> no trailing zeros (still valid, <= 2 dp)
    [InlineData("10.50", "10.5")]    // USD -> one decimal, valid
    [InlineData("9.99", "9.99")]     // USD -> two decimals
    [InlineData("5000.00", "5000")]  // JPY stored as .00 -> "5000", NOT the rejected "5000.00"
    [InlineData("0.01", "0.01")]     // smallest USD
    [InlineData("1234.567", "1234.567")] // 3-decimal currency (e.g. TND) preserved
    public void Money_serializes_as_a_string_without_excess_decimals(string input, string expectedWire)
    {
        var amount = new AmountWithBreakdown { CurrencyCode = "USD", Value = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture) };
        var json = JObject.Parse(JsonConvert.SerializeObject(amount, Settings));

        Assert.Equal(JTokenType.String, json["value"]!.Type); // string on the wire, never a JSON number
        Assert.Equal(expectedWire, (string)json["value"]!);
    }

    [Fact]
    public void Money_round_trips_from_a_paypal_string()
    {
        var json = "{\"currency_code\":\"USD\",\"value\":\"12.34\"}";
        var amount = JsonConvert.DeserializeObject<AmountWithBreakdown>(json, Settings)!;
        Assert.Equal(12.34m, amount.Value);
        Assert.Equal("12.34", ValueOf(amount));
    }

    [Fact]
    public void Japanese_yen_whole_amount_serializes_with_no_decimal_point()
    {
        var amount = new AmountWithBreakdown { CurrencyCode = "JPY", Value = 5000m };
        Assert.Equal("5000", ValueOf(amount));
        Assert.DoesNotContain(".", ValueOf(amount));
    }

    // One realistic amount per currency, asserting the exact wire string that PayPal will accept
    // (never more decimals than the currency allows). Uses the typed PayPalCurrency constants.
    [Theory]
    // USD (2 decimals): typical prices, and a whole amount that must not become "19.00".
    [InlineData(PayPalCurrency.Usd, "19.99", "19.99")]
    [InlineData(PayPalCurrency.Usd, "20.00", "20")]
    [InlineData(PayPalCurrency.Usd, "0.05", "0.05")]
    // GBP (2 decimals): same rules as USD.
    [InlineData(PayPalCurrency.Gbp, "14.50", "14.5")]
    [InlineData(PayPalCurrency.Gbp, "100.00", "100")]
    [InlineData(PayPalCurrency.Gbp, "7.01", "7.01")]
    // JPY (0 decimals): must serialize as a bare integer even when the decimal carries .00.
    [InlineData(PayPalCurrency.Jpy, "5000", "5000")]
    [InlineData(PayPalCurrency.Jpy, "5000.00", "5000")]
    [InlineData(PayPalCurrency.Jpy, "150", "150")]
    public void Currency_amounts_serialize_within_currency_decimals(string currency, string input, string expectedWire)
    {
        var amount = new AmountWithBreakdown { CurrencyCode = currency, Value = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture) };
        var json = JObject.Parse(JsonConvert.SerializeObject(amount, Settings));

        Assert.Equal(JTokenType.String, json["value"]!.Type);
        Assert.Equal(expectedWire, (string)json["value"]!);
    }

    // A JPY value with fractional yen has no valid representation; make sure we never silently
    // truncate money. It serializes with the fraction so PayPal reports the error, not us.
    [Fact]
    public void Fractional_yen_is_not_silently_truncated()
    {
        var amount = new AmountWithBreakdown { CurrencyCode = PayPalCurrency.Jpy, Value = 100.5m };
        Assert.Equal("100.5", ValueOf(amount));
    }

    // The whole model round-trips: build an order request with USD/GBP/JPY money, serialize with the
    // client's settings, and confirm each value is a correctly-scaled JSON string and reads back exactly.
    [Theory]
    [InlineData(PayPalCurrency.Usd, "12.34", "12.34")]
    [InlineData(PayPalCurrency.Gbp, "9.90", "9.9")]
    [InlineData(PayPalCurrency.Jpy, "2500.00", "2500")]
    public void Order_request_money_round_trips_per_currency(string currency, string input, string expectedWire)
    {
        var value = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        var order = new OrderRequest
        {
            Intent = PayPalIntent.Capture,
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    Amount = new AmountWithBreakdown { CurrencyCode = currency, Value = value },
                },
            },
        };

        var json = JObject.Parse(JsonConvert.SerializeObject(order, Settings));
        var wire = json["purchase_units"]![0]!["amount"]!["value"]!;
        Assert.Equal(JTokenType.String, wire.Type);
        Assert.Equal(expectedWire, (string)wire!);

        var parsed = JsonConvert.DeserializeObject<OrderRequest>(json.ToString(), Settings)!;
        Assert.Equal(value, parsed.PurchaseUnits.First().Amount.Value);
        Assert.Equal(currency, parsed.PurchaseUnits.First().Amount.CurrencyCode);
    }
}
