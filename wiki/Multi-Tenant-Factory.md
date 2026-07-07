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
    public Task<Order> CreateOrderFor(Merchant m, OrderRequest order) =>
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

One shared transport across all accounts (no socket exhaustion), one cached client per credential set
(so a merchant's OAuth token is reused, not refetched).

Thread-safe and isolated: each credential set gets its own token cache, merchant context, and handlers;
only the stateless TCP transport is shared, so concurrent multi-merchant use never leaks one merchant's
token into another's calls.

## Getting the access token

```csharp
string bearer = await client.Tokens.GetAccessTokenAsync();   // DI or factory clients
```

## Bring your own token

If another system owns token acquisition, build from a token instead of a client id/secret:

```csharp
// fixed token (no refresh - rebuild when it rotates)
var client = factory.CreateWithAccessToken(accessToken, PayPalEnvironment.Live);

// your own token source (keeps refreshing)
var client = factory.CreateWithTokenProvider(
    new DelegatePayPalTokenProvider(ct => myTokenService.GetPayPalTokenAsync(merchantId, ct)),
    PayPalEnvironment.Live);
```

`IPayPalTokenProvider` is the seam (`StaticPayPalTokenProvider`, `DelegatePayPalTokenProvider`, or your
own). In DI, register your own before `AddPayPalSharp` to replace the built-in provider everywhere. Reuse
the returned client rather than rebuilding per request.
