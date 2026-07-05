namespace Aeroverra.PayPalSharp;

/// <summary>
/// The HTTP header names PayPal sends on a webhook notification, used for offline signature
/// verification. Header lookups by the verifier are case-insensitive.
/// </summary>
public static class PayPalWebhookHeaderNames
{
    public const string TransmissionId = "PAYPAL-TRANSMISSION-ID";
    public const string TransmissionTime = "PAYPAL-TRANSMISSION-TIME";
    public const string TransmissionSig = "PAYPAL-TRANSMISSION-SIG";
    public const string CertUrl = "PAYPAL-CERT-URL";
    public const string AuthAlgo = "PAYPAL-AUTH-ALGO";
}
