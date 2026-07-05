using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    [JsonProperty("id")] public string? Id { get; set; }
    [JsonProperty("event_version")] public string? EventVersion { get; set; }
    [JsonProperty("create_time")] public string? CreateTime { get; set; }
    [JsonProperty("resource_type")] public string? ResourceType { get; set; }
    [JsonProperty("resource_version")] public string? ResourceVersion { get; set; }
    [JsonProperty("event_type")] public string? EventType { get; set; }
    [JsonProperty("summary")] public string? Summary { get; set; }
    [JsonProperty("resource")] public JObject? Resource { get; set; }
    [JsonProperty("links")] public JArray? Links { get; set; }

    /// <summary>Deserializes <see cref="Resource"/> into <typeparamref name="T"/> (null if there is no resource).</summary>
    public T? ResourceAs<T>() where T : class => Resource?.ToObject<T>();

    // Keep PayPal's raw date strings verbatim so the parsed model round-trips faithfully.
    private static readonly JsonSerializerSettings Settings = new()
    {
        DateParseHandling = DateParseHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <summary>Parses a raw webhook request body into a <see cref="PayPalWebhookEvent"/>.</summary>
    public static PayPalWebhookEvent Parse(string json)
        => JsonConvert.DeserializeObject<PayPalWebhookEvent>(json, Settings) ?? new PayPalWebhookEvent();
}
