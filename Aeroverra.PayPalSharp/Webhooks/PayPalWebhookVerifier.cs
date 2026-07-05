using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>The outcome of verifying a webhook. Never throws for a failed verification.</summary>
public sealed class PayPalWebhookVerificationResult
{
    private PayPalWebhookVerificationResult(bool isValid, string? failureReason)
    {
        IsValid = isValid;
        FailureReason = failureReason;
    }

    public bool IsValid { get; }

    /// <summary>Why verification failed (null when valid).</summary>
    public string? FailureReason { get; }

    public static PayPalWebhookVerificationResult Success { get; } = new(true, null);
    public static PayPalWebhookVerificationResult Fail(string reason) => new(false, reason);
}

/// <summary>
/// Verifies an incoming PayPal webhook offline (no API round-trip): it reconstructs the signed string
/// <c>transmissionId|transmissionTime|webhookId|crc32(body)</c>, fetches PayPal's signing certificate
/// from the advertised (allowlisted, HTTPS) URL, and checks the RSA-SHA256 signature. This is faster
/// and cheaper than the online verify-webhook-signature endpoint and needs no extra API call.
/// </summary>
public interface IPayPalWebhookVerifier
{
    /// <summary>Verifies using the raw request body bytes (exactly as received).</summary>
    Task<PayPalWebhookVerificationResult> VerifyAsync(
        IEnumerable<KeyValuePair<string, string>> headers, byte[] body, string? webhookId = null, CancellationToken cancellationToken = default);

    /// <summary>Verifies using the raw request body string (UTF-8).</summary>
    Task<PayPalWebhookVerificationResult> VerifyAsync(
        IEnumerable<KeyValuePair<string, string>> headers, string body, string? webhookId = null, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class PayPalWebhookVerifier : IPayPalWebhookVerifier
{
    private readonly IPayPalCertificateSource _certificateSource;
    private readonly PayPalOptions _options;

    public PayPalWebhookVerifier(IPayPalCertificateSource certificateSource, IOptions<PayPalOptions> options)
    {
        _certificateSource = certificateSource;
        _options = options.Value;
    }

    public Task<PayPalWebhookVerificationResult> VerifyAsync(
        IEnumerable<KeyValuePair<string, string>> headers, string body, string? webhookId = null, CancellationToken cancellationToken = default)
        => VerifyAsync(headers, Encoding.UTF8.GetBytes(body ?? string.Empty), webhookId, cancellationToken);

    public async Task<PayPalWebhookVerificationResult> VerifyAsync(
        IEnumerable<KeyValuePair<string, string>> headers, byte[] body, string? webhookId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(body);

        var lookup = BuildLookup(headers);
        webhookId ??= _options.WebhookId;
        if (string.IsNullOrWhiteSpace(webhookId))
        {
            return PayPalWebhookVerificationResult.Fail("No webhook id available. Set PayPalOptions.WebhookId or pass one to VerifyAsync.");
        }

        if (!TryGet(lookup, PayPalWebhookHeaderNames.TransmissionId, out var transmissionId)
            || !TryGet(lookup, PayPalWebhookHeaderNames.TransmissionTime, out var transmissionTime)
            || !TryGet(lookup, PayPalWebhookHeaderNames.TransmissionSig, out var transmissionSig)
            || !TryGet(lookup, PayPalWebhookHeaderNames.CertUrl, out var certUrl))
        {
            return PayPalWebhookVerificationResult.Fail("Missing one or more PayPal transmission headers.");
        }

        if (!Uri.TryCreate(certUrl, UriKind.Absolute, out var certificateUri))
        {
            return PayPalWebhookVerificationResult.Fail("The PAYPAL-CERT-URL header is not a valid absolute URL.");
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(transmissionSig);
        }
        catch (FormatException)
        {
            return PayPalWebhookVerificationResult.Fail("The transmission signature is not valid base64.");
        }

        System.Security.Cryptography.X509Certificates.X509Certificate2 certificate;
        try
        {
            // The certificate is owned/cached by the source, so we do not dispose it here.
            certificate = await _certificateSource.GetAsync(certificateUri, cancellationToken).ConfigureAwait(false);
        }
        catch (PayPalWebhookVerificationException ex)
        {
            return PayPalWebhookVerificationResult.Fail(ex.Message);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            return PayPalWebhookVerificationResult.Fail("Could not download the webhook certificate: " + ex.Message);
        }

        var crc = PayPalCrc32.Compute(body);
        var expected = $"{transmissionId}|{transmissionTime}|{webhookId}|{crc}";

        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is null)
        {
            return PayPalWebhookVerificationResult.Fail("The webhook certificate does not contain an RSA public key.");
        }

        var valid = rsa.VerifyData(
            Encoding.UTF8.GetBytes(expected), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return valid
            ? PayPalWebhookVerificationResult.Success
            : PayPalWebhookVerificationResult.Fail("The signature does not match the webhook body.");
    }

    private static Dictionary<string, string> BuildLookup(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            if (!map.ContainsKey(header.Key))
            {
                map[header.Key] = header.Value;
            }
        }
        return map;
    }

    private static bool TryGet(Dictionary<string, string> lookup, string name, out string value)
        => lookup.TryGetValue(name, out value!) && !string.IsNullOrWhiteSpace(value);
}

/// <summary>Thrown for unrecoverable problems while preparing to verify a webhook (bad cert URL, unparseable cert).</summary>
public sealed class PayPalWebhookVerificationException : Exception
{
    public PayPalWebhookVerificationException(string message) : base(message) { }
    public PayPalWebhookVerificationException(string message, Exception innerException) : base(message, innerException) { }
}
