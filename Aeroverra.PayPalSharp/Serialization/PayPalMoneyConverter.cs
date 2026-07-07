using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Serializes money amounts as JSON strings (PayPal requires <c>"10.00"</c>, not a bare number) while
/// they are exposed as <see cref="decimal"/> in C#. No float or precision loss.
///
/// Currency handling: PayPal's value string may have at most the currency's number of decimal places
/// (JPY 0, USD 2, TND 3); FEWER is always accepted (<c>"10"</c> == 10.00). So on write we trim trailing
/// zeros, which keeps every well-scaled amount valid for its currency - crucially, a zero-decimal
/// currency like JPY stored as <c>5000.00m</c> serializes as <c>"5000"</c>, not the rejected
/// <c>"5000.00"</c>. Genuinely over-precise input is left as-is so PayPal reports the real error rather
/// than us silently rounding money.
///
/// Registered for decimal by PayPalJsonSettings; System.Text.Json also uses it for decimal?.
/// </summary>
public sealed class PayPalMoneyConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var text = reader.GetString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return 0m;
                }
                return decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);

            case JsonTokenType.Number:
                // Tolerate a numeric value if PayPal ever returns money as a number.
                return reader.GetDecimal();

            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing a money value.");
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        // "0.############" trims trailing zeros (5000.00 -> "5000", 10.50 -> "10.5") and never adds a
        // trailing decimal point, so the result never exceeds the currency's max decimal places.
        writer.WriteStringValue(value.ToString("0.############", CultureInfo.InvariantCulture));
    }
}
