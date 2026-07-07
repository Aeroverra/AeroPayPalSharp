using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Collapses PayPal's <c>allOf: [{$ref: X}, {description: ...}]</c> wrappers down to a bare
/// <c>{$ref: X}</c>. PayPal wraps almost every shared component in a single-ref <c>allOf</c> (usually
/// just to attach a description), and NSwag turns each occurrence into a NEW empty subclass
/// (<c>class Amount3 : Amount_with_breakdown {}</c>), so a handful of real models explode into
/// <c>Amount</c>, <c>Amount2</c>, ..., <c>Payee2</c>, <c>Name5</c>, and so on - 200+ empty aliases.
///
/// Replacing the wrapper with the bare ref makes NSwag use the real component type directly, which
/// removes the aliases with zero behavioral change (the aliases carried no data). We ONLY collapse when
/// it is provably an alias: exactly one <c>$ref</c> member, and every other member is cosmetic
/// (description/title/example/readOnly/...). If any sibling adds <c>properties</c>, <c>required</c>,
/// <c>additionalProperties</c>, or another composition keyword, the schema genuinely extends the base
/// and is left untouched (it stays a distinct, honestly-numbered type).
/// </summary>
public sealed class CollapseSingleRefAllOf : ITransformer
{
    // Keys on a non-$ref allOf member that are purely cosmetic and safe to drop when collapsing.
    private static readonly HashSet<string> CosmeticKeys = new(StringComparer.Ordinal)
    {
        "description", "title", "example", "examples", "readOnly", "writeOnly",
        "deprecated", "nullable", "xml", "externalDocs", "default",
    };

    private int _collapsed;

    public void Transform(JObject openApi)
    {
        _collapsed = 0;
        // Iterate to a fixed point: collapsing one wrapper can expose a nested one.
        while (CollapsePass(openApi) > 0)
        {
        }
        System.Console.WriteLine($"   CollapseSingleRefAllOf: collapsed {_collapsed} single-ref allOf wrapper(s)");
    }

    private int CollapsePass(JToken node)
    {
        var collapsedHere = 0;

        if (node is JObject obj)
        {
            if (obj["allOf"] is JArray allOf && TryGetSoleRef(allOf, out var reference))
            {
                // Replace the whole schema object with just the $ref.
                obj.RemoveAll();
                obj["$ref"] = reference;
                _collapsed++;
                return 1;
            }

            foreach (var property in obj.Properties().ToList())
            {
                collapsedHere += CollapsePass(property.Value);
            }
        }
        else if (node is JArray array)
        {
            foreach (var item in array.ToList())
            {
                collapsedHere += CollapsePass(item);
            }
        }

        return collapsedHere;
    }

    private static bool TryGetSoleRef(JArray allOf, out string reference)
    {
        reference = string.Empty;
        string? foundRef = null;

        foreach (var member in allOf)
        {
            if (member is not JObject memberObj)
            {
                return false;
            }

            if (memberObj["$ref"] is JValue refValue && refValue.Type == JTokenType.String)
            {
                // More than one ref -> a real intersection, not an alias.
                if (foundRef is not null)
                {
                    return false;
                }
                foundRef = (string)refValue!;
                continue;
            }

            // A non-ref member is only OK if every key on it is cosmetic.
            foreach (var key in memberObj.Properties())
            {
                if (!CosmeticKeys.Contains(key.Name))
                {
                    return false;
                }
            }
        }

        if (foundRef is null)
        {
            return false;
        }

        reference = foundRef;
        return true;
    }
}
