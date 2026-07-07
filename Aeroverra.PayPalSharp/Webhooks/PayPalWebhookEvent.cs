using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// A PayPal webhook notification envelope, as your endpoint receives it. The <see cref="Resource"/>
/// shape varies by <see cref="EventType"/> (PayPal does not publish per-event schemas), so it is kept
/// as raw JSON; deserialize it to a concrete type with <see cref="ResourceAs{T}"/> when you know the
/// event, for example <c>evt.ResourceAs&lt;Aeroverra.PayPalSharp.OrdersV2.Order&gt;()</c> for a
/// checkout-order event.
/// </summary>
public sealed class PayPalWebhookEvent
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("event_version")] public string? EventVersion { get; set; }
    [JsonPropertyName("create_time")] public string? CreateTime { get; set; }
    [JsonPropertyName("resource_type")] public string? ResourceType { get; set; }
    [JsonPropertyName("resource_version")] public string? ResourceVersion { get; set; }
    [JsonPropertyName("event_type")] public string? EventType { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("resource")] public JsonElement? Resource { get; set; }
    [JsonPropertyName("links")] public JsonElement? Links { get; set; }

    /// <summary>Deserializes <see cref="Resource"/> into <typeparamref name="T"/> (null if there is no resource).</summary>
    public T? ResourceAs<T>() where T : class
        => Resource is { } resource ? resource.Deserialize<T>(Options) : null;

    private static readonly JsonSerializerOptions Options = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        // System.Text.Json keeps date-like strings verbatim (unlike Newtonsoft), so the raw model
        // round-trips faithfully with no special date handling.
        var options = new JsonSerializerOptions();
        PayPalJsonSettings.Apply(options);
        return options;
    }

    /// <summary>Parses a raw webhook request body into a <see cref="PayPalWebhookEvent"/>.</summary>
    public static PayPalWebhookEvent Parse(string json)
        => JsonSerializer.Deserialize<PayPalWebhookEvent>(json, Options) ?? new PayPalWebhookEvent();
}
