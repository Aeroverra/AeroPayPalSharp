# Configuration

All settings live on `PayPalOptions`, configured in `AddPayPalSharp`.

| Option | Type | Purpose |
|---|---|---|
| `Environment` | `PayPalEnvironment` | `Sandbox` (default) or `Live`. Picks the base URL. |
| `ClientId` | `string` | OAuth2 client id of your REST app. Store in user-secrets or env vars. |
| `ClientSecret` | `string` | OAuth2 client secret. Store in user-secrets or env vars. |
| `PartnerAttributionId` | `string?` | Partner BN code, sent as `PayPal-Partner-Attribution-Id` on every call. |
| `MerchantId` | `string?` | Sub-merchant id used to build `PayPal-Auth-Assertion`. |
| `SendAuthAssertion` | `bool` | When true, attach `PayPal-Auth-Assertion` (act as `MerchantId`) on every call. Default false. |
| `WebhookId` | `string?` | Your webhook id, for signature verification. |
| `BaseUrlOverride` | `string?` | Override the environment-derived base URL. |
| `TimeoutSeconds` | `int` | Total per-call budget incl. retries (default 100). |
| `Retry` | `PayPalRetryOptions` | Safe auto-retry (`MaxRetries`, `BaseDelay`, `MaxDelay`). [Details](Resilience-and-Observability.md). |
| `Logging` | `PayPalLoggingOptions` | Optional `ILogger` request logging (`Enabled`, `Level`). Off by default. |
| `OnRequest` / `OnResponse` / `OnException` | `Action<...>?` | Per-attempt callbacks. [Details](Resilience-and-Observability.md). |
| `PrimaryHandlerFactory` | `Func<HttpMessageHandler>?` | Transport handler for a proxy, custom TLS, connection limits, or a test double. Applies to DI + factory. |
| `ConfigureHttpClient` | `Action<HttpClient>?` | Configure the API `HttpClient` (timeout, default headers). Runs last, so it wins. |

## Proxy and transport

The system/environment proxy (`HTTP_PROXY` / `HTTPS_PROXY` / `NO_PROXY`) is honored with no configuration.
For an explicit proxy (or custom TLS, connection limits, client certs), build the transport handler:

```csharp
services.AddPayPalSharp(o =>
{
    o.ClientId = "..."; o.ClientSecret = "...";
    o.PrimaryHandlerFactory = () => new SocketsHttpHandler
    {
        Proxy = new WebProxy("http://corp-proxy:8080") { Credentials = CredentialCache.DefaultNetworkCredentials },
        UseProxy = true,
    };
});
```

This applies on both the DI and factory paths (including the OAuth token fetch). `ConfigureHttpClient` is
the seam for client-level settings like timeout and default headers.

## Inline

```csharp
services.AddPayPalSharp(o =>
{
    o.Environment  = PayPalEnvironment.Sandbox;
    o.ClientId     = "...";
    o.ClientSecret = "...";
    o.PartnerAttributionId = "YourPartner_SP_PPCP"; // optional
});
```

## From configuration

```csharp
builder.Services.AddPayPalSharp(builder.Configuration); // binds the "PayPal" section
```

`appsettings.json` should hold only non-secret settings:

```json
{
  "PayPal": {
    "Environment": "Sandbox",
    "PartnerAttributionId": "YourPartner_SP_PPCP"
  }
}
```

Provide the secrets out of source control:

```bash
dotnet user-secrets set "PayPal:ClientId" "..."
dotnet user-secrets set "PayPal:ClientSecret" "..."
# or env vars: PayPal__ClientId / PayPal__ClientSecret
```

Never commit `ClientId` / `ClientSecret`.
