using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Central System.Text.Json options for the generated clients. Each generated client routes its
/// <c>UpdateJsonSerializerSettings</c> partial into here (see GeneratedClientHooks.cs).
/// </summary>
public static class PayPalJsonSettings
{
    /// <summary>Applies the shared PayPal serialization rules to <paramref name="options"/>.</summary>
    public static void Apply(JsonSerializerOptions options)
    {
        // Don't send nulls - PayPal treats an absent property and an explicit null differently and
        // generally ignores/rejects explicit nulls.
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Match Newtonsoft's leniency so PayPal's occasional inconsistencies don't fail at runtime:
        // read property names case-insensitively, and accept a number that arrives as a JSON string.
        options.PropertyNameCaseInsensitive = true;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;

        // Money is a decimal in C# but a string on the wire (currency-safe, trims trailing zeros).
        // A converter registered for decimal is also used for decimal? by System.Text.Json.
        options.Converters.Add(new PayPalMoneyConverter());
    }
}
