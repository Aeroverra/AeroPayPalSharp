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
| `TimeoutSeconds` | `int` | Per-request timeout (default 100). |

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
