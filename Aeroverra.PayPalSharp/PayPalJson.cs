using System.Text.Json;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Serialize and deserialize PayPal models to and from PayPal's exact JSON wire format (snake_case field
/// names like <c>id</c>/<c>purchase_units</c>, money as strings). Use this instead of your own serializer
/// when you need the raw JSON of a request or response - for example to store the raw response, forward it
/// to a browser, or log it.
///
/// The SDK models are System.Text.Json-attributed, so serializing them with Newtonsoft (or default
/// options) would emit the C# PascalCase names and break the payload. Re-serializing a model you got back
/// from the SDK is faithful to what PayPal sent: fields the model does not name are preserved through the
/// models' JSON extension-data, so nothing is lost.
/// </summary>
public static class PayPalJson
{
    /// <summary>The System.Text.Json options the SDK uses (wire field names, money as strings).</summary>
    public static JsonSerializerOptions Options { get; } = Build();

    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions();
        PayPalJsonSettings.Apply(options);
        return options;
    }

    /// <summary>Serializes a PayPal model to its JSON wire format (for example a raw response to store or forward).</summary>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Deserializes PayPal JSON into a model.</summary>
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}
