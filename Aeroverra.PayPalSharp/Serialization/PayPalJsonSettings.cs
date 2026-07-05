using Newtonsoft.Json;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Central Newtonsoft settings for the generated clients. PayPal treats an absent
/// property and a <c>null</c> property differently and generally rejects/ignores
/// explicit nulls, so we omit them from request bodies. Each generated client routes
/// its <c>UpdateJsonSerializerSettings</c> partial into here (see GeneratedClientHooks.cs).
/// </summary>
public static class PayPalJsonSettings
{
    /// <summary>Applies the shared PayPal serialization rules to <paramref name="settings"/>.</summary>
    public static void Apply(JsonSerializerSettings settings)
    {
        // Don't send nulls - PayPal prefers omitted properties.
        settings.NullValueHandling = NullValueHandling.Ignore;
        // Keep PayPal's date strings sane; ISO round-trip.
        settings.DateParseHandling = DateParseHandling.DateTimeOffset;
        settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
    }
}
