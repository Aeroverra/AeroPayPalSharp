using System.Text;
using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;

namespace Aeroverra.PayPalSharp.WrapperGenerator;

/// <summary>
/// Produces a single client class per spec (the spec's own ClassName) with tidy
/// method names derived from PayPal's dotted operationIds. The leading resource
/// segment is dropped when it matches the client's resource so methods read
/// naturally: <c>orders.create</c> -> <c>client.Orders.Create()</c>,
/// <c>orders.track.create</c> -> <c>client.Orders.TrackCreate()</c>,
/// <c>event-types.list</c> (under Webhooks) -> <c>client.Webhooks.EventTypesList()</c>.
/// </summary>
internal sealed class PayPalOperationNameGenerator : IOperationNameGenerator
{
    private readonly string _resourceKey;

    public PayPalOperationNameGenerator(string resourceKey) => _resourceKey = resourceKey;

    // Single client per spec - NSwag uses the configured ClassName.
    public bool SupportsMultipleClients => false;

    public string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
        => string.Empty;

    public string GetOperationName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    {
        var id = operation.OperationId;
        if (string.IsNullOrWhiteSpace(id))
        {
            id = httpMethod + "-" + path;
        }

        // Drop the leading "<resource>." segment when it names this client's resource.
        var firstDot = id.IndexOf('.');
        if (firstDot > 0)
        {
            var head = id.Substring(0, firstDot);
            if (string.Equals(Normalize(head), Normalize(_resourceKey), System.StringComparison.OrdinalIgnoreCase))
            {
                id = id.Substring(firstDot + 1);
            }
        }

        var name = Pascal(id);
        return string.IsNullOrEmpty(name) ? Pascal(operation.OperationId ?? (httpMethod + path)) : name;
    }

    private static string Normalize(string value)
        => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string Pascal(string value)
    {
        var parts = value.Split(new[] { '.', '-', '_', '/', ' ', '{', '}' }, System.StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                sb.Append(part.Substring(1));
            }
        }
        return sb.ToString();
    }
}
