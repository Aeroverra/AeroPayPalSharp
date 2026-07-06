using System.Globalization;
using Newtonsoft.Json;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Serializes money amounts as JSON strings (PayPal requires <c>"10.00"</c>, not a bare number) while
/// they are exposed as <see cref="decimal"/> in C#. No float or precision loss.
///
/// Currency handling: PayPal's value string may have at most the currency's number of decimal places
/// (JPY 0, USD 2, TND 3); FEWER is always accepted (<c>"10"</c> == 10.00). So on write we trim trailing
/// zeros, which keeps every well-scaled amount valid for its currency - crucially, a zero-decimal
/// currency like JPY stored as <c>5000.00m</c> serializes as <c>"5000"</c>, not the rejected
/// <c>"5000.00"</c>. Genuinely over-precise input (for example 3 decimals on a USD amount) is left as-is
/// so PayPal reports the real error rather than us silently rounding money.
///
/// Registered for decimal/decimal? by PayPalJsonSettings; only money values are decimal in the generated
/// models, so nothing else is affected.
/// </summary>
public sealed class PayPalMoneyConverter : JsonConverter
{
    public override bool CanConvert(System.Type objectType)
        => objectType == typeof(decimal) || objectType == typeof(decimal?);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is decimal amount)
        {
            // "0.############" trims trailing zeros (5000.00 -> "5000", 10.50 -> "10.5") and never adds a
            // trailing decimal point, so the result never exceeds the currency's max decimal places.
            writer.WriteValue(amount.ToString("0.############", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNull();
        }
    }

    public override object? ReadJson(JsonReader reader, System.Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var nullable = objectType == typeof(decimal?);
        switch (reader.TokenType)
        {
            case JsonToken.Null:
                return nullable ? (decimal?)null : 0m;

            case JsonToken.String:
                var text = (string?)reader.Value;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return nullable ? (decimal?)null : 0m;
                }
                return decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);

            case JsonToken.Integer:
            case JsonToken.Float:
                // Tolerate a numeric value if PayPal ever returns money as a number.
                return System.Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture);

            default:
                throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing a money value.");
        }
    }
}
