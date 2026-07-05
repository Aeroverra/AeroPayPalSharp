using System.Text;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Builds the <c>PayPal-Auth-Assertion</c> header - an unsigned (alg=none) JWT that
/// tells PayPal "I, the partner (<c>iss</c> = client id), am calling on behalf of this
/// sub-merchant (<c>payer_id</c> = merchant id)". PayPal accepts it unsigned for the
/// client-credentials partner model. See PayPal's "multiparty" / auth-assertion docs.
/// </summary>
public static class PayPalAuthAssertion
{
    /// <summary>Builds an auth-assertion value for the given partner client id and sub-merchant id.</summary>
    public static string Build(string clientId, string merchantId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("clientId is required.", nameof(clientId));
        }
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            throw new ArgumentException("merchantId is required.", nameof(merchantId));
        }

        var header = Base64Url("{\"alg\":\"none\"}");
        var payload = Base64Url($"{{\"iss\":\"{clientId}\",\"payer_id\":\"{merchantId}\"}}");
        return $"{header}.{payload}.";
    }

    // JWT uses base64url without padding.
    private static string Base64Url(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
