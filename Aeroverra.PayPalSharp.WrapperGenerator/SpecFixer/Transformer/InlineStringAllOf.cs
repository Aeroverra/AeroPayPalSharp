using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Collapses PayPal's very common pattern of wrapping a scalar reference in an allOf just
/// to attach a description:
/// <code>{ "allOf": [ { "$ref": ".../some_string_type" }, { "description": "..." } ] }</code>
/// After <see cref="FlattenEnumsToString"/> turns those referenced enum schemas into plain
/// strings, NSwag would generate <c>class Foo : string</c> for the wrapper, which does not
/// compile (you cannot derive from the sealed <c>string</c> type). When the single ref
/// resolves to a string primitive, we inline it to a plain string property (keeping the
/// format and description) so NSwag emits a normal <c>string</c> / <c>DateTimeOffset</c>.
/// Object/composition allOf is left untouched.
/// </summary>
public sealed class InlineStringAllOf : ITransformer
{
    private JObject? _schemas;
    private int _inlined;

    public void Transform(JObject openApi)
    {
        _schemas = openApi["components"]?["schemas"] as JObject;
        _inlined = 0;
        Walk(openApi);
        if (_inlined > 0)
        {
            System.Console.WriteLine($"   InlineStringAllOf: inlined {_inlined} string allOf wrapper(s).");
        }
    }

    private void Walk(JToken token)
    {
        switch (token)
        {
            case JObject obj:
                TryInline(obj);
                foreach (var property in obj.Properties().ToList())
                {
                    Walk(property.Value);
                }
                break;
            case JArray array:
                foreach (var item in array)
                {
                    Walk(item);
                }
                break;
        }
    }

    private void TryInline(JObject node)
    {
        if (node["allOf"] is not JArray allOf)
        {
            return;
        }

        var refs = allOf.OfType<JObject>().Where(o => o["$ref"] is not null).ToList();
        var others = allOf.OfType<JObject>().Where(o => o["$ref"] is null).ToList();
        if (refs.Count != 1)
        {
            return;
        }

        var target = ResolveLocalRef(refs[0]["$ref"]!.Value<string>());
        if (target is null || !IsStringPrimitive(target))
        {
            return;
        }

        node.Remove("allOf");
        node["type"] = "string";
        if (target["format"] is JToken format)
        {
            node["format"] = format.DeepClone();
        }
        if (node["description"] is null)
        {
            var description = others.Select(o => o["description"]).FirstOrDefault(d => d is not null)
                              ?? target["description"];
            if (description is not null)
            {
                node["description"] = description.DeepClone();
            }
        }

        _inlined++;
    }

    private JObject? ResolveLocalRef(string? reference)
    {
        const string prefix = "#/components/schemas/";
        if (_schemas is null || reference is null || !reference.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            return null;
        }

        return _schemas[reference[prefix.Length..]] as JObject;
    }

    private static bool IsStringPrimitive(JObject schema)
        => schema["type"]?.Value<string>() == "string"
           && schema["properties"] is null
           && schema["allOf"] is null
           && schema["oneOf"] is null
           && schema["anyOf"] is null;
}
