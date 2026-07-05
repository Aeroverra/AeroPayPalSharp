using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Tightens PayPal's over-nullable models by marking a curated set of fields
/// <c>required</c> - but ONLY fields we've confirmed are present in every response
/// that deserializes into that schema. PayPal marks almost nothing required, so
/// without this every property is nullable and callers null-check things that are
/// always there (a webhook's id, a create-referral response's links, ...).
///
/// Safety rules (so we never "kill the whole thing" on an edge case):
///  * Only add to <c>required</c> if the property actually exists on the schema.
///  * Only list fields that are always present in RESPONSES. Request-only models
///    tolerate absence because we serialize (and omit nulls), never deserialize them.
///  * Keep the list conservative and evidence-based; grow it as tests confirm fields.
/// </summary>
public sealed class MarkKnownRequired : ITransformer
{
    // schema name (as it appears in components.schemas) -> properties known non-null in responses.
    private static readonly IReadOnlyDictionary<string, string[]> KnownRequired = new Dictionary<string, string[]>
    {
        // Webhooks: a persisted/returned webhook always carries its id, url and event_types.
        ["webhook"] = new[] { "id", "url", "event_types" },
        ["event_type"] = new[] { "name" },
        ["event_type_list"] = new[] { "event_types" },
        ["webhook_list"] = new[] { "webhooks" },

        // Partner Referrals: the create response always returns HATEOAS links.
        ["create_referral_data_response"] = new[] { "links" },
        ["link_description"] = new[] { "href", "rel" },
    };

    private int _added;

    public void Transform(JObject openApi)
    {
        _added = 0;
        if (openApi["components"]?["schemas"] is not JObject schemas)
        {
            return;
        }

        foreach (var (schemaName, props) in KnownRequired)
        {
            if (schemas[schemaName] is not JObject schema || schema["properties"] is not JObject properties)
            {
                continue;
            }

            foreach (var prop in props)
            {
                if (properties[prop] is null)
                {
                    continue; // property not on this schema - skip, don't break generation
                }

                MarkRequired(schema, prop);
            }
        }

        if (_added > 0)
        {
            System.Console.WriteLine($"   MarkKnownRequired: marked {_added} property(ies) required.");
        }
    }

    private void MarkRequired(JObject schema, string prop)
    {
        if (schema["required"] is not JArray required)
        {
            required = new JArray();
            schema["required"] = required;
        }

        if (required.Any(t => string.Equals(t.Value<string>(), prop, System.StringComparison.Ordinal)))
        {
            return;
        }

        required.Add(prop);
        _added++;
    }
}
