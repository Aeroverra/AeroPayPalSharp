using System.Text;
using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace Aeroverra.PayPalSharp.WrapperGenerator.Naming;

/// <summary>
/// Converts PayPal's snake_case JSON names into idiomatic C# PascalCase for BOTH property names and type
/// names (<c>currency_code</c> -> <c>CurrencyCode</c>, <c>amount_with_breakdown</c> -> <c>AmountWithBreakdown</c>).
/// NSwag still emits <c>[JsonProperty("currency_code")]</c> on every property, so the wire format is
/// unchanged - this is purely C# ergonomics.
/// </summary>
public static class PascalCaseNaming
{
    public static string ToPascalCase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name ?? string.Empty;
        }

        var builder = new StringBuilder(name!.Length);
        var capitalizeNext = true;
        foreach (var ch in name)
        {
            if (ch is '_' or '-' or ' ' or '.' or '/' or '+')
            {
                capitalizeNext = true;
                continue;
            }
            if (!char.IsLetterOrDigit(ch))
            {
                continue;
            }
            builder.Append(capitalizeNext ? char.ToUpperInvariant(ch) : ch);
            capitalizeNext = false;
        }

        if (builder.Length == 0)
        {
            return name!;
        }
        // A C# identifier cannot start with a digit.
        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }
        return builder.ToString();
    }
}

/// <summary>PascalCases property names. NSwag keeps the JsonProperty attribute, so the wire name is preserved.</summary>
public sealed class PascalCasePropertyNameGenerator : IPropertyNameGenerator
{
    public string Generate(JsonSchemaProperty property) => PascalCaseNaming.ToPascalCase(property.Name);
}

/// <summary>PascalCases generated type names; the base still deduplicates genuine collisions with a numeric suffix.</summary>
public sealed class PascalCaseTypeNameGenerator : DefaultTypeNameGenerator
{
    public override string Generate(JsonSchema schema, string? typeNameHint, System.Collections.Generic.IEnumerable<string> reservedTypeNames)
        => base.Generate(schema, PascalCaseNaming.ToPascalCase(typeNameHint), reservedTypeNames);
}
