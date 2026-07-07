# Partner and platform (multiparty)

Two partner headers are attached automatically when configured:

- `PayPal-Partner-Attribution-Id` (your BN code) - sent on every request when `PartnerAttributionId` is set.
- `PayPal-Auth-Assertion` - makes a call run on behalf of a sub-merchant.

## Acting on behalf of a seller

Two separate things:

- **Who gets the money** = the order's `payee.merchant_id` (request body).
- **Acting as the seller** = the `PayPal-Auth-Assertion` header, set with an `ActingAsMerchant` scope.

```csharp
using (paypal.ActingAsMerchant(sellerMerchantId))
{
    var order = new OrderRequest
    {
        Intent = PayPalIntent.Capture,
        PurchaseUnits = new List<PurchaseUnitRequest>
        {
            new PurchaseUnitRequest
            {
                Amount = new AmountWithBreakdown { CurrencyCode = PayPalCurrency.Usd, Value = 10.00m },
                Payee  = new Payee { MerchantId = sellerMerchantId },   // who gets the money
            },
        },
    };
    await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
}
```

The scope is thread-safe: it sets an `AsyncLocal`, so concurrent requests never see each other's merchant
and a single injected client serves many sellers. One rule: **`await` inside the `using`** (do not
fire-and-forget across the scope).

Other ways to set the assertion:

- Globally for a single-seller client: `SendAuthAssertion = true` + `MerchantId` in options.
- Per call: pass `payPal_Auth_Assertion`, built with `PayPalAuthAssertion.Build(clientId, merchantId)`.

A header you set yourself always wins over the automatic one.

## Onboarding a seller (partner referrals)

Create a referral, then send the seller to its `action_url`:

```csharp
var referral = await paypal.PartnerReferralsV2.CreateAsync(new ReferralData
{
    TrackingId = trackingId,
    PartnerConfigOverride = new PartnerConfigOverride { ReturnUrl = new Uri(returnUrl) },
    Products = new ProductList { "PPCP" },
    // operations + legal_consents ...
});
var actionUrl = referral.Links.First(l => l.Rel == "action_url").Href;
```

**Do not** rely on the browser redirecting back to `return_url` - PayPal treats it as best-effort and it
often does not fire. Confirm completion with:

- the `MERCHANT.ONBOARDING.COMPLETED` webhook (best for a server), or
- polling `PartnerReferralsV1.MerchantIntegrationFindAsync(partnerMerchantId, trackingId)` (404 = not yet).

Check `PaymentsReceivable` and `PrimaryEmailConfirmed` on the merchant-integration to know the seller can
transact.
