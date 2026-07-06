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

## Thread safety and isolation

The factory is fully thread-safe. Building clients for many merchants from many threads at once is safe
and never leaks one merchant's auth into another's calls: each credential set gets its **own** token
provider and cache, its **own** merchant context, and its **own** configured handlers. The only shared
thing is the raw TCP transport, which holds no credential state. Building the same credential set
concurrently returns one cached client (construction happens once). This is covered by a test that hits
five merchants from hundreds of concurrent threads and asserts each client holds only its own token.

## Getting the access token

Every client exposes its token source:

```csharp
string bearer = await client.Tokens.GetAccessTokenAsync();
```

This works on both DI-injected and factory-built clients. Use it to call something the SDK does not yet
wrap, or to hand a token to another system.

## Bring your own token

If another system owns token acquisition (a central token service, a cache, or a non
client-credentials OAuth flow), build a client from a token instead of a client id/secret:

```csharp
// A token you already hold (no refresh - build a new client when it rotates):
var client = factory.CreateWithAccessToken(accessToken, PayPalEnvironment.Live);

// Or your own token source, so one client keeps fetching/refreshing:
var client = factory.CreateWithTokenProvider(
    new DelegatePayPalTokenProvider(async ct => await myTokenService.GetPayPalTokenAsync(merchantId, ct)),
    PayPalEnvironment.Live);
```

`IPayPalTokenProvider` is the seam. Ship-in implementations are `StaticPayPalTokenProvider` (a fixed
token) and `DelegatePayPalTokenProvider` (your callback); implement the interface yourself for anything
more. In DI, registering your own `IPayPalTokenProvider` before `AddPayPalSharp` replaces the built-in
client-credentials provider everywhere. Reuse a bring-your-own-token client rather than building one per
request; prefer the delegate provider when the token rotates so a single client stays valid.
