using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Fixes a PayPal spec bug: <c>GET /v1/customer/partners/{partner_id}/merchant-integrations</c> (find a
/// seller's integration by tracking id) declares its success (201) response with only a description and
/// no schema, so the generated method returns <c>Task</c> (no body). It actually returns a
/// <c>merchant-integration</c>. This gives the success response the correct schema so the method returns
/// <c>Task&lt;MerchantIntegration&gt;</c>. No-op for every other spec.
/// </summary>
public sealed class FixMerchantIntegrationFindResponse : ITransformer
{
    private const string FindPath = "/v1/customer/partners/{partner_id}/merchant-integrations";
    private const string CorrectRef = "#/components/schemas/merchant-integration";

    public void Transform(JObject openApi)
    {
        if (openApi["paths"]?[FindPath]?["get"]?["responses"] is not JObject responses)
        {
            return;
        }

        // PayPal returns the seller's integration on success; the code is 201 here (200 handled too for safety).
        foreach (var code in new[] { "200", "201" })
        {
            if (responses[code] is JObject response)
            {
                response["content"] = new JObject
                {
                    ["application/json"] = new JObject
                    {
                        ["schema"] = new JObject { ["$ref"] = CorrectRef },
                    },
                };
            }
        }
    }
}
