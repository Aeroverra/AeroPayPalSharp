using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Removes OpenAPI <c>callbacks</c> from operations. Callbacks describe out-of-band
/// async callback requests (nested path-item / operation objects keyed by runtime
/// expressions); they are irrelevant to generating a request client and NSwag's parser
/// trips over PayPal's use of them (it ends up deserializing a real path-item as an
/// operation and throws "Required property 'responses' not found"). Dropping them is safe.
/// </summary>
public sealed class StripCallbacks : ITransformer
{
    private int _removed;

    public void Transform(JObject openApi)
    {
        _removed = 0;
        Strip(openApi);
        if (_removed > 0)
        {
            System.Console.WriteLine($"   StripCallbacks: removed {_removed} callbacks block(s).");
        }
    }

    private void Strip(JToken token)
    {
        switch (token)
        {
            case JObject obj:
                if (obj.Remove("callbacks"))
                {
                    _removed++;
                }
                foreach (var property in obj.Properties().ToList())
                {
                    Strip(property.Value);
                }
                break;
            case JArray array:
                foreach (var item in array)
                {
                    Strip(item);
                }
                break;
        }
    }
}
