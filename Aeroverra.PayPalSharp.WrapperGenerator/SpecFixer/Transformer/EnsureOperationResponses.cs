using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Some PayPal operations ship without a <c>responses</c> object (for example
/// payments reauthorize), which is invalid OpenAPI and makes NSwag's parser throw
/// ("Required property 'responses' not found"). Backfill a minimal default response
/// on any operation that is missing one so generation can proceed.
/// </summary>
public sealed class EnsureOperationResponses : ITransformer
{
    private static readonly string[] HttpMethods =
        { "get", "put", "post", "delete", "options", "head", "patch", "trace" };

    private int _added;

    public void Transform(JObject openApi)
    {
        _added = 0;
        if (openApi["paths"] is not JObject paths)
        {
            return;
        }

        foreach (var pathProperty in paths.Properties())
        {
            if (pathProperty.Value is not JObject pathItem)
            {
                continue;
            }

            foreach (var method in HttpMethods)
            {
                if (pathItem[method] is JObject operation && operation["responses"] is null)
                {
                    operation["responses"] = new JObject
                    {
                        ["default"] = new JObject { ["description"] = "Default response." },
                    };
                    _added++;
                }
            }
        }

        if (_added > 0)
        {
            System.Console.WriteLine($"   EnsureOperationResponses: backfilled responses on {_added} operation(s).");
        }
    }
}
