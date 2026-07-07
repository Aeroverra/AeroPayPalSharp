# Webhooks

AeroPayPalSharp covers both sides of webhooks: managing subscriptions through the API, and receiving
and verifying incoming notifications on your endpoint.

## Managing subscriptions

Use `client.Webhooks` (the Webhooks Management API):

```csharp
var catalog = await paypal.Webhooks.WebhooksEventTypesListAsync();   // every event type PayPal can send
var mine    = await paypal.Webhooks.ListAsync();                     // your subscriptions

Webhook created = await paypal.Webhooks.PostAsync(new Webhook
{
    Url = new Uri("https://your.app/paypal/webhook"),
    EventTypes = new DefinitionsEventTypeList
    {
        new EventType { Name = PayPalWebhookEventType.PaymentCaptureCompleted },
        new EventType { Name = PayPalWebhookEventType.CheckoutOrderApproved },
    },
});
```

`PayPalWebhookEventType` has a constant for every published event name (and `IsKnown` / `All`), so you
never hand-type `"PAYMENT.CAPTURE.COMPLETED"`.

## Receiving and verifying a webhook (offline)

`IPayPalWebhookVerifier` verifies the RSA signature **offline** (no API round-trip): it fetches PayPal's
signing cert from the allowlisted `PAYPAL-CERT-URL`, caches it, and checks the signature. It is registered
by `AddPayPalSharp`, is framework-agnostic (give it headers + raw body), and uses `PayPalOptions.WebhookId`.

```csharp
app.MapPost("/paypal/webhook", async (HttpRequest request, IPayPalWebhookVerifier verifier) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var headers = request.Headers.Select(h => new KeyValuePair<string, string>(h.Key, h.Value.ToString()));

    var result = await verifier.VerifyAsync(headers, body);
    if (!result.IsValid) return Results.Unauthorized();   // result.FailureReason says why

    var evt = PayPalWebhookEvent.Parse(body);
    if (evt.EventType == PayPalWebhookEventType.CheckoutOrderApproved)
    {
        var order = evt.ResourceAs<Aeroverra.PayPalSharp.OrdersV2.Order>();
        // ...
    }
    return Results.Ok();
});
```

- Verify against the **raw** body exactly as received (there is a `byte[]` overload too).
- `VerifyAsync` never throws for a bad signature; it returns `IsValid = false` + `FailureReason`.
- Certs are fetched over HTTPS from PayPal's hosts only, and cached until they expire.

## The event envelope

`PayPalWebhookEvent.Parse(body)` gives you the typed envelope: `Id`, `EventType`, `ResourceType`,
`EventVersion`, `Summary`, `CreateTime`, `Links`, and the raw `Resource`. PayPal does not publish a
schema per event type, so `Resource` is raw JSON; call `ResourceAs<T>()` with the type you expect for
that event (for example `OrdersV2.Order` for a checkout-order event).

## The certificate endpoint

The cert-download endpoint is not in PayPal's published specs, so it lives in this repo's
hand-maintained spec (see [Regenerating the clients](Regenerating.md)) and is exposed as
`client.Custom.CertsGetAsync(certId)` if you ever need to fetch a certificate by id yourself. The
verifier fetches the exact advertised URL directly, so you normally do not need to call this.
