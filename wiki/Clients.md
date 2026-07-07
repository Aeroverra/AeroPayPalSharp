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
var order = new OrderRequest
{
    Intent = PayPalIntent.Capture,
    PurchaseUnits = new List<PurchaseUnitRequest>
    {
        new PurchaseUnitRequest { Amount = new AmountWithBreakdown { CurrencyCode = PayPalCurrency.Usd, Value = 10.00m } },
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
    EventTypes = new DefinitionsEventTypeList
    {
        new EventType { Name = "PAYMENT.CAPTURE.COMPLETED" },
    },
});
await paypal.Webhooks.DeleteAsync(created.Id);
```

To verify an incoming webhook, prefer the offline `IPayPalWebhookVerifier` (no API round-trip) - see
[Webhooks](Webhooks.md). The online API call is also available as
`paypal.Webhooks.VerifyWebhookSignaturePostAsync(new VerifyWebhookSignature { ... })`.

## Partner Referrals v2

```csharp
CreateReferralDataResponse created = await paypal.PartnerReferralsV2.CreateAsync(referralData);
string actionUrl = created.Links.First(l => l.Rel == "action_url").Href;   // seller onboarding URL
```
