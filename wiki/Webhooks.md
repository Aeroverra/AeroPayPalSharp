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

When PayPal POSTs a notification to your endpoint you must verify it is genuine. PayPal offers an online
`verify-webhook-signature` API call, but the faster and cheaper way is to verify the RSA signature
**offline**, with no extra API round-trip. `IPayPalWebhookVerifier` does exactly that: it rebuilds the
signed string `transmissionId|transmissionTime|webhookId|crc32(body)`, downloads PayPal's signing
certificate from the (allowlisted, HTTPS) URL in the `PAYPAL-CERT-URL` header, caches it, and checks
the signature.

It is registered by `AddPayPalSharp` and is framework-agnostic: give it the request headers and the
raw body. Set your webhook id once via `PayPalOptions.WebhookId` (or pass it per call).

ASP.NET Core minimal API example:

```csharp
app.MapPost("/paypal/webhook", async (HttpRequest request, IPayPalWebhookVerifier verifier) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    var headers = request.Headers.Select(h => new KeyValuePair<string, string>(h.Key, h.Value.ToString()));
    var result = await verifier.VerifyAsync(headers, body);
    if (!result.IsValid)
    {
        return Results.Unauthorized();   // result.FailureReason says why
    }

    var evt = PayPalWebhookEvent.Parse(body);
    switch (evt.EventType)
    {
        case PayPalWebhookEventType.PaymentCaptureCompleted:
            var capture = evt.ResourceAs<Aeroverra.PayPalSharp.PaymentsV2.Capture2>();
            // ...
            break;
    }
    return Results.Ok();
});
```

Notes:

- Verify against the **raw** body bytes exactly as received. Read the body before any model binding
  reframes it (there is a `byte[]` overload if you have the exact bytes).
- `VerifyAsync` never throws for a bad signature; it returns `IsValid = false` with a `FailureReason`.
- The certificate URL host must be one of PayPal's, and the fetch is over HTTPS, so the certificate
  comes from a TLS-authenticated PayPal endpoint. Certificates are cached until they expire.

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
