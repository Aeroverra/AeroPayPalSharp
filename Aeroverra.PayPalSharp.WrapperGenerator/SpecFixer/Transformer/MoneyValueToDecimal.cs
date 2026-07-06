using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Retypes PayPal money <c>value</c> fields from string to decimal so amounts are strongly typed in C#.
/// PayPal always pairs a money value with a sibling <c>currency_code</c>, so any object schema that has
/// BOTH <c>currency_code</c> and <c>value</c> properties is a money object; its <c>value</c> becomes
/// <c>{ type: number, format: decimal }</c> (NSwag then emits <c>decimal</c>).
///
/// The wire format stays a JSON STRING (PayPal requires money as a string, not a number) via
/// PayPalMoneyConverter in the library; .NET decimal preserves scale so "10.00" round-trips exactly.
/// The specs have no other bare <c>number</c> fields, so only money values become decimal and the
/// converter is safe.
/// </summary>
public sealed class MoneyValueToDecimal : ITransformer
{
    private int _retyped;

    public void Transform(JObject openApi)
    {
        _retyped = 0;
        Walk(openApi);
        System.Console.WriteLine($"   MoneyValueToDecimal: retyped {_retyped} money value field(s) to decimal");
    }

    private void Walk(JToken node)
    {
        if (node is JObject obj)
        {
            if (obj["properties"] is JObject props
                && props["currency_code"] != null
                && props["value"] is JObject valueSchema)
            {
                RetypeToDecimal(valueSchema);
            }

            foreach (var property in obj.Properties())
            {
                Walk(property.Value);
            }
        }
        else if (node is JArray array)
        {
            foreach (var item in array)
            {
                Walk(item);
            }
        }
    }

    private void RetypeToDecimal(JObject valueSchema)
    {
        var description = (string?)valueSchema["description"];

        // Drop string-only keywords, then set number/decimal.
        foreach (var key in new[] { "type", "format", "pattern", "maxLength", "minLength", "$ref", "allOf", "enum" })
        {
            valueSchema.Remove(key);
        }
        valueSchema["type"] = "number";
        valueSchema["format"] = "decimal";
        if (description != null)
        {
            valueSchema["description"] = description;
        }

        _retyped++;
    }
}
