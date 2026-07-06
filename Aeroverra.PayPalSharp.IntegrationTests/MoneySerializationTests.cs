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

    private static string ValueOf(Amount3 amount) => (string)JObject.Parse(JsonConvert.SerializeObject(amount, Settings))["value"]!;

    [Theory]
    [InlineData("10.00", "10")]      // whole USD amount -> no trailing zeros (still valid, <= 2 dp)
    [InlineData("10.50", "10.5")]    // USD -> one decimal, valid
    [InlineData("9.99", "9.99")]     // USD -> two decimals
    [InlineData("5000.00", "5000")]  // JPY stored as .00 -> "5000", NOT the rejected "5000.00"
    [InlineData("0.01", "0.01")]     // smallest USD
    [InlineData("1234.567", "1234.567")] // 3-decimal currency (e.g. TND) preserved
    public void Money_serializes_as_a_string_without_excess_decimals(string input, string expectedWire)
    {
        var amount = new Amount3 { Currency_code = "USD", Value = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture) };
        var json = JObject.Parse(JsonConvert.SerializeObject(amount, Settings));

        Assert.Equal(JTokenType.String, json["value"]!.Type); // string on the wire, never a JSON number
        Assert.Equal(expectedWire, (string)json["value"]!);
    }

    [Fact]
    public void Money_round_trips_from_a_paypal_string()
    {
        var json = "{\"currency_code\":\"USD\",\"value\":\"12.34\"}";
        var amount = JsonConvert.DeserializeObject<Amount3>(json, Settings)!;
        Assert.Equal(12.34m, amount.Value);
        Assert.Equal("12.34", ValueOf(amount));
    }

    [Fact]
    public void Japanese_yen_whole_amount_serializes_with_no_decimal_point()
    {
        var amount = new Amount3 { Currency_code = "JPY", Value = 5000m };
        Assert.Equal("5000", ValueOf(amount));
        Assert.DoesNotContain(".", ValueOf(amount));
    }
}
