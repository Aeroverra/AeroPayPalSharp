# Partner and platform (multiparty)

Two partner headers are added automatically by `PayPalPartnerHeaderHandler`:

- **`PayPal-Partner-Attribution-Id`** (your BN code) is sent on every request when `PartnerAttributionId` is set.
- **`PayPal-Auth-Assertion`** makes a call run on behalf of a sub-merchant.

## Acting on behalf of a seller

Two separate things, and it is easy to conflate them:

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
        Intent = PayPalIntent.Capture,
        Purchase_units = new List<Purchase_units>
        {
            new Purchase_units
            {
                Amount = new Amount3 { Currency_code = PayPalCurrency.Usd, Value = "10.00" },
                Payee  = new Payee3 { Merchant_id = sellerMerchantId },   // who gets the money
            },
        },
    };

    Order created = await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
}
```

## Thread safety of the scope

`ActingAsMerchant` does not create a new client; it sets an ambient value on an `AsyncLocal<string?>`
(the same primitive Serilog's `LogContext` uses). `AsyncLocal` stores its value in the current async
execution context, not on the shared client or handler, so:

- Concurrent requests each read their own flow's value. Two threads in different scopes at the same
  time never see each other's merchant, and a thread making direct (unscoped) calls sends no assertion
  even while other threads are scoped.
- The partner header handler reads the value at send time (it does not capture a copy), so a single
  pooled handler shared across all requests stays correct.
- Nested scopes restore the outer value on dispose.

This is covered by a test that drives 900 interleaved concurrent flows through one shared client and
asserts none leak. One rule makes it hold: **`await` your calls inside the `using`** (the natural
pattern above). If you start a task and let the `using` dispose before it runs, the scope is already
gone. Do not fire-and-forget across the scope boundary.

## Other ways to set the assertion

- **Globally**, for a client dedicated to one seller: set `SendAuthAssertion = true` + `MerchantId` in
  options (or on `PayPalCredentials` when building via the factory). Every call then acts as that merchant.
- **Explicitly per call**, where PayPal exposes it: many methods take a `payPal_Auth_Assertion` argument.
  Build the value with `PayPalAuthAssertion.Build(clientId, merchantId)` if you want to pass it yourself.

Per-call headers you set yourself always win; the handler only fills in what you did not.

Many create/capture methods also accept a `payPal_Request_Id` (idempotency) argument and a `prefer`
argument (pass `"return=representation"` for the full object in the response).

## Onboarding sellers (partner referrals)

To onboard a seller, create a partner referral with `PartnerReferralsV2.CreateAsync` (a `tracking_id`
you choose, `operations`, `products`, `legal_consents`, and a `partner_config_override.return_url`),
then send the seller to the `action_url` link from the response.

Do **not** rely on the browser redirecting back to your `return_url` to know that onboarding finished.
PayPal treats that redirect as best-effort and it is increasingly unreliable (it may not fire at all).
The authoritative completion signals are:

- The **`MERCHANT.ONBOARDING.COMPLETED`** webhook (and `CUSTOMER.MERCHANT-INTEGRATION.*` events) - the
  best choice for a server, since it needs no polling. Verify it with the
  [webhook verifier](Webhooks.md).
- **Polling the merchant-integration status by tracking id**:
  `PartnerReferralsV1.MerchantIntegrationFindAsync(partnerMerchantId, trackingId)`. A 404 means not yet;
  a 200 means an integration exists for that seller. Read `payments_receivable` and
  `primary_email_confirmed` on the response to confirm the seller can actually transact.

The interactive onboarding test (`InteractiveFlowTests`) demonstrates the polling approach.
