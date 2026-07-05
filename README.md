# AeroPayPalSharp

A strongly-typed .NET client for the [PayPal REST APIs](https://developer.paypal.com/api/rest/),
generated from [PayPal's official OpenAPI specifications](https://github.com/paypal/paypal-rest-api-specifications)
with NSwag and a transformer pipeline that fixes PayPal's spec quirks and tightens the models.

You register it once in your DI container, inject a single `IPayPalApiClient`, and call
resource-scoped sub-clients:

```csharp
public sealed class Onboarding(IPayPalApiClient paypal)
{
    public Task<WebhookList> ListWebhooks() => paypal.Webhooks.ListAsync();

    public Task<Create_referral_data_response> ReferSeller(Referral_data data)
        => paypal.PartnerReferralsV2.CreateAsync(data);
}
```

- **Target framework:** `net10.0`
- **JSON:** Newtonsoft.Json (PayPal-tuned - nulls omitted from request bodies)
- **Auth:** OAuth2 client-credentials, fetched + cached + refreshed automatically
- **Partner/platform ready:** partner-attribution and auth-assertion headers built in

---

## Table of contents

- [Install](#install)
- [Get PayPal credentials](#get-paypal-credentials)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [How authentication works](#how-authentication-works)
- [Partner &amp; platform (multiparty)](#partner--platform-multiparty)
- [Multi-tenant: clients from raw credentials](#multi-tenant-clients-from-raw-credentials)
- [Using the clients](#using-the-clients)
  - [Webhooks](#webhooks)
  - [Partner Referrals v2](#partner-referrals-v2)
  - [Partner Referrals v1](#partner-referrals-v1-legacy)
- [Error handling](#error-handling)
- [Models &amp; nullability](#models--nullability)
- [Regenerating the clients](#regenerating-the-clients)
- [Adding a new PayPal API](#adding-a-new-paypal-api)
- [Testing](#testing)
- [Project layout](#project-layout)
- [Roadmap](#roadmap)

---

## Install

Reference the `Aeroverra.PayPalSharp` project (or package, once published). It pulls in
Newtonsoft.Json and the `Microsoft.Extensions.*` DI/HTTP/Options packages it needs.

```xml
<ProjectReference Include="..\Aeroverra.PayPalSharp\Aeroverra.PayPalSharp.csproj" />
```

## Get PayPal credentials

1. Go to the [PayPal Developer dashboard](https://developer.paypal.com/dashboard/applications/sandbox)
   and create a **REST API app**. You get a **Client ID** and **Secret** for both Sandbox and Live.
2. For partner/platform integrations you'll also have a **BN code** (partner attribution id) and,
   when acting for sub-merchants, their **merchant id**.
3. Keep the client id/secret out of source control - user-secrets, environment variables, or a
   secret store (see [Configuration](#configuration)).

## Quick start

```csharp
using Aeroverra.PayPalSharp;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddPayPalSharp(options =>
{
    options.Environment  = PayPalEnvironment.Sandbox;   // or Live
    options.ClientId     = "AXG3...";                   // from user-secrets / env in real code
    options.ClientSecret = "EBcu...";
    options.PartnerAttributionId = "YourPartner_SP_PPCP"; // optional BN code
});

var provider = services.BuildServiceProvider();
var paypal   = provider.GetRequiredService<IPayPalApiClient>();

// list the webhook event types PayPal can send you
var catalog = await paypal.Webhooks.WebhooksEventTypesListAsync();
Console.WriteLine($"{catalog.Event_types.Count} event types available");
```

## Configuration

Everything lives on `PayPalOptions`:

| Option | Type | Purpose |
|---|---|---|
| `Environment` | `PayPalEnvironment` | `Sandbox` (default) or `Live`. Picks the base URL. |
| `ClientId` | `string` | OAuth2 client id of your REST app. **Secret - user-secrets/env.** |
| `ClientSecret` | `string` | OAuth2 client secret. **Secret - user-secrets/env.** |
| `PartnerAttributionId` | `string?` | BN code, sent as `PayPal-Partner-Attribution-Id` on every call. |
| `MerchantId` | `string?` | Sub-merchant id, used to build `PayPal-Auth-Assertion`. |
| `SendAuthAssertion` | `bool` | When true, attach `PayPal-Auth-Assertion` to every call (act as `MerchantId`). Default false. |
| `WebhookId` | `string?` | Your webhook id, for signature verification. |
| `BaseUrlOverride` | `string?` | Override the environment-derived base URL. |
| `TimeoutSeconds` | `int` | Per-request timeout (default 100). |

### Binding from configuration

```csharp
// Program.cs
builder.Services.AddPayPalSharp(builder.Configuration); // binds the "PayPal" section
```

`appsettings.json` (non-secret only):

```json
{
  "PayPal": {
    "Environment": "Sandbox",
    "PartnerAttributionId": "YourPartner_SP_PPCP"
  }
}
```

Secrets via user-secrets (development) or environment variables (production):

```bash
dotnet user-secrets set "PayPal:ClientId" "AXG3..."
dotnet user-secrets set "PayPal:ClientSecret" "EBcu..."
# or:  PayPal__ClientId=... PayPal__ClientSecret=...   (env vars)
```

> **Never commit credentials.** `appsettings.json` should only hold non-secret settings.

## How authentication works

You never touch tokens. `AddPayPalSharp` wires:

- **`PayPalTokenProvider`** - POSTs `grant_type=client_credentials` to `/v1/oauth2/token` with HTTP
  Basic (`clientId:clientSecret`), caches the access token, and refreshes it ~60s before expiry. A
  semaphore collapses concurrent refreshes into a single request.
- **`PayPalAuthenticationHandler`** - a `DelegatingHandler` that attaches
  `Authorization: Bearer <token>` to every request from every sub-client.

If you ever need a raw token (e.g. to call an endpoint not yet wrapped), inject the provider:

```csharp
public sealed class Raw(IPayPalTokenProvider tokens)
{
    public async Task<string> Bearer() => await tokens.GetAccessTokenAsync();
}
```

## Partner &amp; platform (multiparty)

Two partner headers are added automatically by `PayPalPartnerHeaderHandler`:

- **`PayPal-Partner-Attribution-Id`** (your BN code) is sent on every request when `PartnerAttributionId` is set.
- **`PayPal-Auth-Assertion`** makes a call run on behalf of a sub-merchant.

### Acting on behalf of a seller

When you process for a seller there are two separate things, and it is easy to conflate them:

- **Who receives the money** is the order's `payee.merchant_id`, in the request **body** (not a header).
- **Acting as the seller** (using the permissions they granted your platform) is the
  `PayPal-Auth-Assertion` **header**.

The cleanest way to send that header is a scope on the client. Everything inside the `using` runs on
behalf of that merchant, and the value flows across awaits and is restored on dispose, so a single
injected client serves many sellers safely (including concurrently):

```csharp
using (paypal.ActingAsMerchant(sellerMerchantId))
{
    var order = new Order_request
    {
        Intent = "CAPTURE",
        Purchase_units = new List<Purchase_units>
        {
            new Purchase_units
            {
                Amount = new Amount3 { Currency_code = "USD", Value = "10.00" },
                Payee  = new Payee3 { Merchant_id = sellerMerchantId },   // who gets the money
            },
        },
    };

    Order created = await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
}
```

Other ways to set the assertion:

- **Globally**, for a client dedicated to one seller: set `SendAuthAssertion = true` + `MerchantId` in
  options (or on `PayPalCredentials` when building via the factory). Every call then acts as that merchant.
- **Explicitly per call**, where PayPal exposes it: many methods take a `payPal_Auth_Assertion` argument.
  Build the value with `PayPalAuthAssertion.Build(clientId, merchantId)` if you want to pass it yourself.

Per-call headers you set yourself always win; the handler only fills in what you did not.

> Many create/capture methods also accept a `payPal_Request_Id` (idempotency) argument and a `prefer`
> argument (pass `"return=representation"` for the full object in the response).

## Multi-tenant: clients from raw credentials

If you are NOT a single configured account (for example a service that processes payments for many
merchants, each with their own PayPal client id and secret), use `IPayPalClientFactory` to build a
client on the fly from any credentials. You do not need to register those credentials in DI.

Register the factory (this is also done automatically by `AddPayPalSharp`):

```csharp
services.AddPayPalSharpFactory();   // registers IPayPalClientFactory only
```

Then, in a service, get a client for whichever account you are handling:

```csharp
public sealed class PaymentService(IPayPalClientFactory paypalFactory)
{
    public async Task<WebhookList> MerchantWebhooks(Merchant m)
    {
        IPayPalApiClient client = paypalFactory.Create(
            m.PayPalClientId,
            m.PayPalClientSecret,
            PayPalEnvironment.Live,
            partnerAttributionId: m.BnCode);   // optional

        return await client.Webhooks.ListAsync();
    }
}
```

Or with the full `PayPalCredentials` record (adds merchant id, auth-assertion, base-URL override, timeout):

```csharp
var client = paypalFactory.Create(new PayPalCredentials
{
    ClientId = "...", ClientSecret = "...",
    Environment = PayPalEnvironment.Live,
    PartnerAttributionId = "YourPartner_SP_PPCP",
    MerchantId = "SUBMERCHANTID", SendAuthAssertion = true,
});
```

No DI at all? Just `new` it up:

```csharp
using var factory = new PayPalClientFactory();
var client = factory.Create(clientId, clientSecret, PayPalEnvironment.Sandbox);
```

The factory reuses one shared transport across every account (so it does not exhaust sockets) and
caches one client per distinct credential set, so repeat calls for the same merchant reuse the cached
OAuth token instead of fetching a new one each time.

## Using the clients

Inject `IPayPalApiClient` and reach the sub-clients, **or** inject any sub-client interface directly
(`IOrdersV2Client`, `IWebhooksV1Client`, and so on).

All of PayPal's REST APIs are wrapped, each as a sub-client:

| `IPayPalApiClient` member | PayPal API | Example calls |
|---|---|---|
| `.Orders` | Orders v2 | `CreateAsync`, `GetAsync`, `CaptureAsync`, `AuthorizeAsync`, `ConfirmAsync` |
| `.Payments` | Payments v2 | `AuthorizationsGetAsync`, `CapturesRefundAsync`, `RefundsGetAsync` |
| `.Invoices` | Invoicing v2 | `CreateAsync`, `SendAsync`, `ListAsync`, `RemindAsync`, `CancelAsync` |
| `.Subscriptions` | Subscriptions v1 | `CreateAsync`, `GetAsync`, `CancelAsync`, `PlansListAsync`, `PlansCreateAsync` |
| `.CatalogProducts` | Catalog Products v1 | `CreateAsync`, `ListAsync`, `GetAsync`, `PatchAsync` |
| `.Disputes` | Disputes v1 | `ListAsync`, `GetAsync`, `ProvideEvidenceAsync`, `AppealAsync`, `AcceptClaimAsync` |
| `.Payouts` | Payouts v1 | `PostAsync`, `GetAsync`, `PayoutsItemGetAsync`, `PayoutsItemCancelAsync` |
| `.TransactionSearch` | Transaction Search v1 | `SearchGetAsync`, `BalancesGetAsync` |
| `.ShipmentTracking` | Add Tracking v1 | `PostAsync`, `PutAsync`, `GetAsync`, `TrackersBatchPostAsync` |
| `.PaymentTokens` | Payment Method Tokens v3 | `CreateAsync`, `GetAsync`, `DeleteAsync`, `SetupTokensCreateAsync` |
| `.WebProfiles` | Payment Experience v1 | `CreateAsync`, `GetListAsync`, `UpdateAsync`, `DeleteAsync` |
| `.PartnerReferralsV2` | Partner Referrals v2 | `CreateAsync`, `ReadAsync` |
| `.PartnerReferralsV1` | Partner Referrals v1 (deprecated) | `CreateAsync`, `MerchantIntegrationStatusAsync` |
| `.Webhooks` | Webhooks Management v1 | `ListAsync`, `PostAsync`, `WebhooksEventTypesListAsync`, `VerifyWebhookSignaturePostAsync` |

A few of these are shown in detail below; the rest follow the same shape.

### Webhooks

`client.Webhooks` (`IWebhooksV1Client`) - manage subscriptions, browse event types, inspect and
resend event notifications, and verify signatures.

```csharp
var wh = paypal.Webhooks;

// --- event type catalog (everything PayPal can send) ---
EventTypeList catalog = await wh.WebhooksEventTypesListAsync();

// --- create a subscription ---
Webhook created = await wh.PostAsync(new Webhook
{
    Url = new Uri("https://your.app/paypal/webhook"),
    Event_types = new DefinitionsEvent_type_list
    {
        new Event_type { Name = "PAYMENT.CAPTURE.COMPLETED" },
        new Event_type { Name = "CHECKOUT.ORDER.APPROVED" },
    },
});

// --- read / list ---
Webhook one      = await wh.GetAsync(created.Id);
WebhookList mine = await wh.ListAsync();

// --- the event types a given webhook is subscribed to ---
EventTypeList subscribed = await wh.EventTypesListAsync(created.Id);

// --- event notifications (history) ---
EventList events = await wh.WebhooksEventsListAsync();
Event evt        = await wh.WebhooksEventsGetAsync("<event-id>");
await wh.WebhooksEventsResendAsync("<event-id>", /* resend body */ null);

// --- simulate an event (great for testing your handler) ---
Event simulated = await wh.SimulateEventPostAsync(/* SimulateEvent body */ null);

// --- delete ---
await wh.DeleteAsync(created.Id);
```

**Verify a webhook signature** (server-side, when you receive a webhook):

```csharp
var result = await paypal.Webhooks.VerifyWebhookSignaturePostAsync(new Verify_webhook_signature
{
    Auth_algo         = request.Headers["PAYPAL-AUTH-ALGO"],
    Cert_url          = new Uri(request.Headers["PAYPAL-CERT-URL"]),
    Transmission_id   = request.Headers["PAYPAL-TRANSMISSION-ID"],
    Transmission_sig  = request.Headers["PAYPAL-TRANSMISSION-SIG"],
    Transmission_time = DateTimeOffset.Parse(request.Headers["PAYPAL-TRANSMISSION-TIME"]),
    Webhook_id        = options.WebhookId,
    Webhook_event     = deserializedEvent,
});

if (!string.Equals(result.Verification_status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
    return Unauthorized();
```

### Partner Referrals v2

`client.PartnerReferralsV2` - onboard sellers to PayPal Complete Payments. `CreateAsync` returns
HATEOAS links; send the seller to the `action_url`, then poll status.

```csharp
var referral = new Referral_data
{
    Tracking_id = "seller-42",
    Operations = new Operation_list
    {
        new Operation
        {
            Operation1 = "API_INTEGRATION",   // NSwag renamed the "operation" field -> Operation1
            Api_integration_preference = new Integration_details
            {
                Rest_api_integration = new Rest_api_integration
                {
                    Integration_method = "PAYPAL",
                    Integration_type   = "THIRD_PARTY",
                    Third_party_details = new Third_party_details
                    {
                        Features = new Rest_api_integration_rest_endpoint_features_enum_list
                        {
                            "PAYMENT", "REFUND",
                        },
                    },
                },
            },
        },
    },
    Products = new Product_list { "EXPRESS_CHECKOUT" },
    Legal_consents = new Legal_consent_list
    {
        new Legal_consent { Type = "SHARE_DATA_CONSENT", Granted = true },
    },
};

Create_referral_data_response created = await paypal.PartnerReferralsV2.CreateAsync(referral);
string actionUrl = created.Links.First(l => l.Rel == "action_url").Href;   // redirect the seller here
string selfUrl   = created.Links.First(l => l.Rel == "self").Href;
string referralId = selfUrl.TrimEnd('/').Split('/').Last();

Referral_data_response read = await paypal.PartnerReferralsV2.ReadAsync(referralId);
```

### Partner Referrals v1 (legacy)

`client.PartnerReferralsV1` - deprecated but included for parity. Adds merchant-integration and
partner-config operations beyond v2:

```csharp
var status = await paypal.PartnerReferralsV1.MerchantIntegrationStatusAsync(/* partner id, merchant id */);
var creds  = await paypal.PartnerReferralsV1.MerchantIntegrationCredentialsAsync(/* ... */);
var found  = paypal.PartnerReferralsV1.MerchantIntegrationFindAsync(/* find by tracking id */);
```

> New integrations should use v2. v1 exists so existing flows keep working.

## Error handling

Every failed request throws a **per-namespace** `PayPalApiException` (e.g.
`Aeroverra.PayPalSharp.WebhooksV1.PayPalApiException`) carrying `StatusCode` and the raw `Response`
body. For responses PayPal documents with an error schema, the **typed subclass**
`PayPalApiException<TError>` also exposes the parsed error as `.Result`.

Because the typed subclass is what actually gets thrown, catch the **base** type (or use
`Assert.ThrowsAny` in tests):

```csharp
try
{
    await paypal.Webhooks.GetAsync(id);
}
catch (PayPalApiException ex) when (ex.StatusCode == 404)
{
    // not found
}
catch (PayPalApiException ex)
{
    _logger.LogError("PayPal {Status}: {Body}", ex.StatusCode, ex.Response);
    throw;
}
```

Token acquisition failures throw `PayPalAuthenticationException`.

## Models &amp; nullability

PayPal's specs mark almost nothing required, so a naive generator makes every property nullable.
Two transformers tighten this while staying crash-proof:

- **Enums are strings.** PayPal adds enum values constantly (payment states, dispute reasons,
  processor codes...). A generated C# enum would throw on any value it hasn't seen, breaking
  deserialization of an otherwise-fine response. So enums become `string`, with the allowed values
  preserved in each property's XML doc comment.
- **Known fields are non-null.** A curated, evidence-based set of always-present *response* fields
  (a webhook's `id`, a create-referral response's `links`, ...) is marked required so you don't
  null-check things that are always there. Request-only and uncertain fields are left nullable so an
  edge case never blows up parsing.

The set grows conservatively as tests confirm more fields (see `MarkKnownRequired`).

## Regenerating the clients

Clients are produced by the `Aeroverra.PayPalSharp.WrapperGenerator` console app (build-time only -
nothing references it, so its NSwag dependencies never reach the shipped library). The raw PayPal
specs live in `WrapperGenerator/Definitions/`; a transformer pipeline runs over each in memory before
NSwag sees it.

```bash
# generate from the committed specs
dotnet run --project Aeroverra.PayPalSharp.WrapperGenerator

# refresh the raw specs from PayPal's GitHub first, then generate
dotnet run --project Aeroverra.PayPalSharp.WrapperGenerator -- --download
```

Output goes to `Aeroverra.PayPalSharp/Generated/*.cs`. Method names are derived from PayPal's dotted
operationIds with the resource prefix dropped: `orders.create` -> `client.Orders.CreateAsync`,
`event-types.list` -> `client.Webhooks.EventTypesListAsync`.

## Adding a new PayPal API

Everything is data-driven, so adding e.g. Orders is a few edits:

1. Add a `ClientSpec` to the `Clients` array in `WrapperGenerator/Program.cs`
   (name, spec file, class name, namespace, output file, resource key, download URL).
2. Run the generator - a new `Generated/OrdersV2Client.cs` appears.
3. Add the `UpdateJsonSerializerSettings` hook for the new namespace in
   `Serialization/GeneratedClientHooks.cs`.
4. Expose it on `IPayPalApiClient` / `PayPalApiClient` (e.g. `.Orders`) and register it in
   `ServiceCollectionExtensions.AddApiClient<...>`.
5. Add any resource-specific transformers (extra `MarkKnownRequired` entries, etc.) and tests.

## Testing

`Aeroverra.PayPalSharp.IntegrationTests` runs against the **real PayPal sandbox**. Credentials come
from user-secrets and are never committed; when they're absent the tests skip (via `SkippableFact`)
instead of failing.

```bash
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:ClientId" "..."
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:ClientSecret" "..."
# optional: PartnerAttributionId, MerchantId, WebhookId

dotnet test
```

Current coverage (all green against sandbox): OAuth token issue + cache; webhook event-type catalog;
webhook list; full create -> get -> list-subscribed -> delete -> 404 round-trip; signature verification;
partner-referral create (asserts `action_url`) + read-back.

## Project layout

| Project | Role |
|---|---|
| `Aeroverra.PayPalSharp` | The SDK you consume - options, auth, DI, the aggregate `PayPalApiClient`, and `Generated/`. |
| `Aeroverra.PayPalSharp.WrapperGenerator` | Regenerates `Generated/*.cs` from the OpenAPI specs (NSwag + transformers). Not shipped. |
| `Aeroverra.PayPalSharp.IntegrationTests` | Live sandbox xUnit tests. |

## Roadmap

All 13 of PayPal's published REST APIs are wrapped. Remaining polish:

- More per-endpoint live tests across the newer clients (Invoices, Subscriptions, Payments, Disputes,
  Payouts, Payment Tokens, and so on).
- A growing `MarkKnownRequired` set as always-present response fields are confirmed per resource.
- Optional cleanup of a few NSwag-numbered type names (for example the renamed `Operation1` field).
