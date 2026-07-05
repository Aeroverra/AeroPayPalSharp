using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer;

/// <summary>
/// A single, focused edit applied to a PayPal OpenAPI document (parsed as a
/// <see cref="JObject"/>) before NSwag turns it into C#. PayPal's published specs
/// are technically valid but generate poor models - everything nullable, enums that
/// break on new values, occasional spec quirks - so each transformer patches one
/// such problem. Transformers run in order; keep each one small and idempotent.
/// </summary>
public interface ITransformer
{
    void Transform(JObject openApi);
}
