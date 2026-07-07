using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Rewrites PayPal's site-relative markdown links inside <c>description</c> text to absolute
/// developer.paypal.com URLs, so the links in the generated XML doc comments actually resolve. PayPal's
/// specs are full of links like <c>[Currency Codes](/api/rest/reference/currency-codes/)</c> that only
/// work on their docs site; as-is they are dead links in an IDE. This fixes the reported doc-link issue
/// (and every other relative link) in one pass. Absolute links and anchors are left untouched.
/// </summary>
public sealed class AbsolutizeDocLinks : ITransformer
{
    private const string Base = "https://developer.paypal.com";

    // Matches a markdown link whose target starts with a single '/' (site-relative), e.g. ](/api/...).
    private static readonly Regex RelativeLink = new(@"\]\((/[^)\s]+)\)", RegexOptions.Compiled);

    // Matches an HTML anchor href to a site-relative path, e.g. href="/docs/..." (PayPal descriptions
    // sometimes use raw HTML instead of markdown).
    private static readonly Regex RelativeHref = new("""href=(["'])(/[^"']+)\1""", RegexOptions.Compiled);

    private int _rewritten;

    public void Transform(JObject openApi)
    {
        _rewritten = 0;
        Walk(openApi);
        System.Console.WriteLine($"   AbsolutizeDocLinks: rewrote {_rewritten} relative doc link(s)");
    }

    private void Walk(JToken node)
    {
        switch (node)
        {
            case JObject obj:
                foreach (var property in obj.Properties())
                {
                    if (property.Name == "description" && property.Value.Type == JTokenType.String)
                    {
                        var original = (string)property.Value!;
                        var updated = RelativeLink.Replace(original, m =>
                        {
                            _rewritten++;
                            return $"]({Base}{m.Groups[1].Value})";
                        });
                        updated = RelativeHref.Replace(updated, m =>
                        {
                            _rewritten++;
                            return $"href={m.Groups[1].Value}{Base}{m.Groups[2].Value}{m.Groups[1].Value}";
                        });
                        if (original != updated)
                        {
                            property.Value = updated;
                        }
                    }
                    else
                    {
                        Walk(property.Value);
                    }
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
}
