# Multi-tenant: clients from raw credentials

If you are not a single configured account (for example a service that processes payments for many
merchants, each with their own PayPal client id and secret), use `IPayPalClientFactory` to build a
client on the fly from any credentials. Those credentials do not need to be registered in DI.

Register the factory (this is also done automatically by `AddPayPalSharp`):

```csharp
services.AddPayPalSharpFactory();   // registers IPayPalClientFactory only
```

Then, in a service, get a client for whichever account you are handling:

```csharp
public sealed class PaymentService(IPayPalClientFactory paypalFactory)
{
    public Task<Order> CreateOrderFor(Merchant m, Order_request order) =>
        paypalFactory
            .Create(m.PayPalClientId, m.PayPalClientSecret, PayPalEnvironment.Live, partnerAttributionId: m.BnCode)
            .Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
}
```

Or with the full `PayPalCredentials` record (adds merchant id, auth-assertion, base-url override, timeout):

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
caches one client per distinct credential set, so repeat calls for the same merchant reuse that
account's cached OAuth token instead of fetching a new one each time.
