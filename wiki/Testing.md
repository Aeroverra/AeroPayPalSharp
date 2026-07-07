# Testing

`Aeroverra.PayPalSharp.IntegrationTests` runs against the real PayPal sandbox. Credentials come from
user-secrets and are never committed; when they are absent the live tests skip (via `SkippableFact`)
instead of failing, so the suite is safe to run in CI.

```bash
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:ClientId" "..."
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:ClientSecret" "..."
# optional, for partner / acting-as-seller tests:
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:PartnerAttributionId" "..."
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:SellerMerchantId" "..."

dotnet test
```

The real-webhook verification test reads captured fixtures (body + headers + signing cert) from a local
folder, and the webhook id they were sent to, from user-secrets so nothing account-specific lives in the
repo. It skips unless both are set:

```bash
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:WebhookFixturesDir" "C:\\path\\to\\fixtures"
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:WebhookId" "your-webhook-id"
```

## Negative testing (forced sandbox errors)

Force the sandbox to return a specific error with a `WithMockResponse` scope. It sends the
`PayPal-Mock-Response` header only inside the block, and **only in Sandbox** (never Live):

```csharp
using (paypal.WithMockResponse(PayPalMockCode.InstrumentDeclined))
{
    await Assert.ThrowsAsync<PayPalApiException>(
        () => paypal.Orders.CaptureAsync(orderId, payPal_Request_Id: $"{orderId}-capture"));
}
```

`PayPalMockCode` has typed constants for the common codes (plus `All` / `IsKnown`); or pass any raw
string, or a full JSON header value. Pass `null` (or empty) to apply no mock, so the `using` can stay in
place unconditionally:

```csharp
using (paypal.WithMockResponse(simulateFailure ? PayPalMockCode.InstrumentDeclined : null))
{
    await paypal.Orders.CaptureAsync(orderId, payPal_Request_Id: $"{orderId}-capture");
}
```

## Interactive (human-in-the-loop) tests

Some endpoints need a real buyer approval that cannot be automated (capturing and refunding a paid
order). `InteractiveFlowTests` covers these: it creates an order, opens your browser to the PayPal
approval page, waits on a throwaway `localhost` listener for you to pay with a sandbox buyer, then
captures and refunds. Every request and response body is logged so you can inspect real payloads.

It never runs in a normal `dotnet test` / CI run; it skips unless you opt in:

```bash
dotnet user-secrets --project Aeroverra.PayPalSharp.IntegrationTests set "PayPal:RunInteractive" "true"
dotnet test --filter "InteractiveFlowTests" --logger "console;verbosity=detailed"
# set it back to false afterwards so ordinary runs do not open a browser
```

## What is covered

- Auth token issue and caching.
- Webhooks: event-type catalog, list, a full create, get, list-subscribed, delete, 404 round-trip, and
  signature verification.
- Orders: create for a seller (with `payee.merchant_id`) and read back, plus create inside an
  `ActingAsMerchant` scope.
- The `ActingAsMerchant` header mechanics, proven by no-network unit tests.
- Partner Referrals v2: create (asserts the `action_url`) and read back.
- Catalog Products and Web Profiles: full create / read / list / delete lifecycles.
- Invoices, Disputes, Subscriptions plans, Transaction Search balances: read smoke tests.
- Payments, Payouts, Payment Tokens: typed-error "wiring" tests (a lookup of an unknown id proves auth,
  routing, and error deserialization end to end).
- The runtime factory: builds a working client from raw credentials and caches per credential set.

Some endpoints need a completed buyer flow (a real capture, dispute, or vaulted token) and cannot be
driven from an automated test alone. Capture and refund are covered by the interactive test above
(a real approved-and-paid order); the rest are covered by the wiring tests rather than a full flow.
