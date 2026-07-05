# The clients

Inject `IPayPalApiClient` and reach the sub-clients, or inject any sub-client interface directly
(`IOrdersV2Client`, `IWebhooksV1Client`, and so on). All of PayPal's REST APIs are wrapped:

| `IPayPalApiClient` member | PayPal API | Example calls |
|---|---|---|
| `.Orders` | Orders v2 | `CreateAsync`, `GetAsync`, `CaptureAsync`, `AuthorizeAsync`, `ConfirmAsync` |
| `.Payments` | Payments v2 | `AuthorizationsGetAsync`, `CapturesRefundAsync`, `RefundsGetAsync` |
| `.Invoices` | Invoicing v2 | `CreateAsync`, `SendAsync`, `ListAsync`, `RemindAsync`, `InvoicingGenerateNextInvoiceNumberAsync` |
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

Method names come from PayPal's dotted operationIds with the resource prefix dropped
(`orders.create` becomes `Orders.CreateAsync`, `event-types.list` becomes `Webhooks.EventTypesListAsync`).

## Orders

```csharp
var order = new Order_request
{
    Intent = PayPalIntent.Capture,
    Purchase_units = new List<Purchase_units>
    {
        new Purchase_units { Amount = new Amount3 { Currency_code = PayPalCurrency.Usd, Value = "10.00" } },
    },
};

Order created = await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
Order fetched = await paypal.Orders.GetAsync(created.Id);
```

To create an order for a seller, see [Partner and platform](Partner-and-Platform.md).

## Webhooks

```csharp
var catalog = await paypal.Webhooks.WebhooksEventTypesListAsync();   // all available event types
var mine    = await paypal.Webhooks.ListAsync();                     // your subscriptions

Webhook created = await paypal.Webhooks.PostAsync(new Webhook
{
    Url = new Uri("https://your.app/paypal/webhook"),
    Event_types = new DefinitionsEvent_type_list
    {
        new Event_type { Name = "PAYMENT.CAPTURE.COMPLETED" },
    },
});
await paypal.Webhooks.DeleteAsync(created.Id);
```

Verify a signature when a webhook arrives:

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

## Partner Referrals v2

```csharp
Create_referral_data_response created = await paypal.PartnerReferralsV2.CreateAsync(referralData);
string actionUrl = created.Links.First(l => l.Rel == "action_url").Href;   // seller onboarding URL
```
