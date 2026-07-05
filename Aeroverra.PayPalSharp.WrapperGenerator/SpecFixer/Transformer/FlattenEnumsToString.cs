using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Replaces every string <c>enum</c> in the document with a plain <c>type: string</c>,
/// moving the allowed values into the description so they stay documented.
///
/// Why: PayPal adds new enum values all the time (payment states, dispute reasons,
/// processor codes...). A generated C# enum throws on any value it hasn't seen, which
/// would blow up deserialization of an otherwise-fine response. Strings never break;
/// the allowed set lives in the XML doc comment for discoverability. This is also
/// what the old generator did - PayPal's enum <c>$ref</c>s otherwise trip NSwag up.
/// </summary>
public sealed class FlattenEnumsToString : ITransformer
{
    private int _flattened;

    public void Transform(JObject openApi)
    {
        _flattened = 0;
        Walk(openApi);
        if (_flattened > 0)
        {
            System.Console.WriteLine($"   FlattenEnumsToString: flattened {_flattened} enum(s) to string.");
        }
    }

    private void Walk(JToken token)
    {
        switch (token)
        {
            case JObject obj:
                TryFlatten(obj);
                // ToList() so we can mutate children while iterating.
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

    private void TryFlatten(JObject node)
    {
        if (node["enum"] is not JArray values || values.Count == 0)
        {
            return;
        }

        // Only touch string enums. PayPal enums are all strings; anything else
        // (rare) is left alone so we don't mangle a numeric/boolean enum.
        var typeToken = node["type"];
        var isStringTyped = typeToken is null || string.Equals(typeToken.Value<string>(), "string", System.StringComparison.Ordinal);
        var allStringValues = values.All(v => v.Type == JTokenType.String);
        if (!isStringTyped || !allStringValues)
        {
            return;
        }

        var allowed = string.Join(", ", values.Select(v => v.Value<string>()));
        node.Remove("enum");
        node["type"] = "string";

        var note = $"Allowed values: {allowed}.";
        var existing = node["description"]?.Value<string>();
        node["description"] = string.IsNullOrWhiteSpace(existing) ? note : existing.TrimEnd() + "\n\n" + note;

        _flattened++;
    }
}
