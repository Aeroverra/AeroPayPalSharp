using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Supplies the PayPal public certificate used to verify a webhook signature, given the URL PayPal
/// advertised in the <c>PAYPAL-CERT-URL</c> header. Abstracted so verification can be unit-tested
/// without network access.
/// </summary>
public interface IPayPalCertificateSource
{
    Task<X509Certificate2> GetAsync(Uri certificateUrl, CancellationToken cancellationToken = default);
}

/// <summary>
/// Fetches (and caches) the signing certificate over HTTPS. The cert URL host must be one of PayPal's
/// (so an attacker cannot point verification at their own cert host), and the transport is HTTPS, so
/// the certificate comes from a TLS-authenticated PayPal endpoint. Certificates are cached by URL and
/// re-fetched once they fall outside their validity window.
/// </summary>
public sealed class HttpPayPalCertificateSource : IPayPalCertificateSource
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "api.paypal.com", "api.sandbox.paypal.com", "api-m.paypal.com", "api-m.sandbox.paypal.com",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, X509Certificate2> _cache = new(StringComparer.Ordinal);

    public HttpPayPalCertificateSource(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<X509Certificate2> GetAsync(Uri certificateUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(certificateUrl);
        if (certificateUrl.Scheme != Uri.UriSchemeHttps || !AllowedHosts.Contains(certificateUrl.Host))
        {
            throw new PayPalWebhookVerificationException(
                $"Refusing to fetch a webhook certificate from an untrusted URL: {certificateUrl}.");
        }

        var key = certificateUrl.AbsoluteUri;
        if (_cache.TryGetValue(key, out var cached) && IsCurrentlyValid(cached))
        {
            return cached;
        }

        var client = _httpClientFactory.CreateClient();
        var pem = await client.GetStringAsync(certificateUrl, cancellationToken).ConfigureAwait(false);

        X509Certificate2 certificate;
        try
        {
            certificate = X509Certificate2.CreateFromPem(pem);
        }
        catch (Exception ex)
        {
            throw new PayPalWebhookVerificationException("The fetched webhook certificate could not be parsed.", ex);
        }

        if (!IsCurrentlyValid(certificate))
        {
            throw new PayPalWebhookVerificationException("The fetched webhook certificate is outside its validity window.");
        }

        _cache[key] = certificate;
        return certificate;
    }

    private static bool IsCurrentlyValid(X509Certificate2 certificate)
    {
        var now = DateTime.Now;
        return now >= certificate.NotBefore && now <= certificate.NotAfter;
    }
}
