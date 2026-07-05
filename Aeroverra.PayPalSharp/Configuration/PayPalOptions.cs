namespace Aeroverra.PayPalSharp;

/// <summary>Which PayPal environment to target.</summary>
public enum PayPalEnvironment
{
    /// <summary>The sandbox (https://api-m.sandbox.paypal.com) - test money only.</summary>
    Sandbox,

    /// <summary>Production (https://api-m.paypal.com) - real money.</summary>
    Live,
}

/// <summary>
/// Configuration for the PayPal clients. Bind from configuration
/// (<see cref="SectionName"/>) or set inline in <c>AddPayPalSharp</c>. Keep the
/// client id/secret out of committed config - use user-secrets / environment vars.
/// </summary>
public sealed class PayPalOptions
{
    public const string SectionName = "PayPal";

    /// <summary>Sandbox or Live. Selects the default base URL.</summary>
    public PayPalEnvironment Environment { get; set; } = PayPalEnvironment.Sandbox;

    /// <summary>OAuth2 client id of your PayPal REST app.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth2 client secret of your PayPal REST app.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Partner attribution id (a.k.a. BN code) sent as <c>PayPal-Partner-Attribution-Id</c>
    /// on every request when set. Required for partner/platform revenue attribution.
    /// </summary>
    public string? PartnerAttributionId { get; set; }

    /// <summary>
    /// The sub-merchant id a partner acts on behalf of. Used to build the
    /// <c>PayPal-Auth-Assertion</c> header when <see cref="SendAuthAssertion"/> is on
    /// (or per-call via <see cref="PayPalAuthAssertion"/>).
    /// </summary>
    public string? MerchantId { get; set; }

    /// <summary>
    /// When true, attach a <c>PayPal-Auth-Assertion</c> (built from <see cref="ClientId"/>
    /// + <see cref="MerchantId"/>) to every request so calls run as the sub-merchant.
    /// Off by default - most partner calls (onboarding, webhooks) act as the partner.
    /// </summary>
    public bool SendAuthAssertion { get; set; }

    /// <summary>Webhook id used for webhook signature verification, if you verify webhooks.</summary>
    public string? WebhookId { get; set; }

    /// <summary>Overrides the environment-derived base URL. Leave null for the default.</summary>
    public string? BaseUrlOverride { get; set; }

    /// <summary>Per-request timeout for the underlying HttpClients.</summary>
    public int TimeoutSeconds { get; set; } = 100;

    /// <summary>The effective base URL (override, else derived from <see cref="Environment"/>).</summary>
    public string BaseUrl => string.IsNullOrWhiteSpace(BaseUrlOverride)
        ? Environment == PayPalEnvironment.Live ? "https://api-m.paypal.com" : "https://api-m.sandbox.paypal.com"
        : BaseUrlOverride!;
}
