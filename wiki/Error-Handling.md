# Error handling

Every failed request throws a single, shared `Aeroverra.PayPalSharp.PayPalApiException` (the same type
for every sub-client, so one `catch` handles an error from Orders, Payments, Webhooks, or any other),
carrying `StatusCode` and the raw `Response` body. For responses PayPal documents with an error schema,
the typed subclass `PayPalApiException<TError>` also exposes the parsed error as `.Result`.

Because the typed subclass is what actually gets thrown, catch the base type:

```csharp
try
{
    await paypal.Orders.GetAsync(id);
}
catch (PayPalApiException ex) when (ex.StatusCode == 404)
{
    // not found
}
catch (PayPalApiException ex)
{
    logger.LogError("PayPal {Status}: {Body}", ex.StatusCode, ex.Response);
    throw;
}
```

Token acquisition failures throw `PayPalAuthenticationException`.
