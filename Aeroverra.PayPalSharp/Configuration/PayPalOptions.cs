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

    /// <summary>
    /// Per-request timeout for the underlying HttpClients. This is the TOTAL budget for one call
    /// including any automatic retries and their backoff waits, so it also bounds the worst-case wait.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 100;

    /// <summary>Automatic, idempotency-safe retry of transient failures. See <see cref="PayPalRetryOptions"/>.</summary>
    public PayPalRetryOptions Retry { get; set; } = new();

    /// <summary>Optional request/response logging. Off by default.</summary>
    public PayPalLoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Called just before each HTTP attempt is sent (after auth/partner headers are attached). A hook for
    /// custom logging, metrics, or header inspection - the .NET-native equivalent of an "API callback".
    /// Keep it fast and non-throwing; exceptions from it are ignored.
    /// </summary>
    public Action<HttpRequestMessage>? OnRequest { get; set; }

    /// <summary>
    /// Called after each HTTP response is received. This includes API errors: a PayPal 4xx/5xx arrives as
    /// a response here (the typed <c>PayPalApiException</c> is synthesized later), so this DOES fire for
    /// them. Use it to read response headers, for example the <c>PayPal-Debug-Id</c> for support tickets
    /// (<see cref="PayPalHeaders.GetDebugId(HttpResponseMessage)"/>). Keep it fast and non-throwing.
    /// </summary>
    public Action<HttpResponseMessage>? OnResponse { get; set; }

    /// <summary>
    /// Called when a request fails with no response at all - a transport failure such as a network error,
    /// timeout, or cancellation (where <see cref="OnResponse"/> cannot fire because nothing came back).
    /// The exception is rethrown afterwards. Keep it fast and non-throwing.
    /// </summary>
    public Action<HttpRequestMessage, Exception>? OnException { get; set; }

    /// <summary>The effective base URL (override, else derived from <see cref="Environment"/>).</summary>
    public string BaseUrl => string.IsNullOrWhiteSpace(BaseUrlOverride)
        ? Environment == PayPalEnvironment.Live ? "https://api-m.paypal.com" : "https://api-m.sandbox.paypal.com"
        : BaseUrlOverride!;
}

/// <summary>
/// Controls the built-in transient-fault retry. The defaults are safe for money: a mutating call
/// (POST/PATCH) is retried ONLY when it carries a <c>PayPal-Request-Id</c> idempotency key, so PayPal
/// deduplicates it and it can never be applied twice. Set <see cref="MaxRetries"/> to 0 to disable.
/// </summary>
public sealed class PayPalRetryOptions
{
    /// <summary>Maximum retry attempts after the first try (0 disables retries entirely). Default 3.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base backoff delay; the wait grows exponentially from here with jitter. Default 500ms.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Upper bound on any single backoff wait (also caps a server <c>Retry-After</c>). Default 5s.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>Controls optional request/response logging via <c>ILogger</c>.</summary>
public sealed class PayPalLoggingOptions
{
    /// <summary>When true, log a one-line summary (method, path, status, elapsed, debug id) per attempt. Default false.</summary>
    public bool Enabled { get; set; }

    /// <summary>Level for the per-request summary. Default <see cref="LogLevelOption.Debug"/>.</summary>
    public LogLevelOption Level { get; set; } = LogLevelOption.Debug;
}

/// <summary>A minimal log-level selector so the options do not force a Microsoft.Extensions.Logging enum on callers.</summary>
public enum LogLevelOption
{
    Trace,
    Debug,
    Information,
    Warning,
}
