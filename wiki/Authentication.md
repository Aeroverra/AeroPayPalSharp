# Authentication

You never handle tokens yourself. `AddPayPalSharp` wires two pieces:

- **`PayPalTokenProvider`** POSTs `grant_type=client_credentials` to `/v1/oauth2/token` with HTTP Basic
  (`clientId:clientSecret`), caches the access token, and refreshes it about 60 seconds before expiry.
  A semaphore collapses concurrent refreshes into a single request.
- **`PayPalAuthenticationHandler`** is a `DelegatingHandler` that attaches
  `Authorization: Bearer <token>` to every request from every sub-client.

## Lifetime: one token, shared

The token provider is registered as a **singleton**, so its cache is shared across every injected client,
every controller/service, and every DI scope. Injecting `IPayPalApiClient` (or any sub-client) into a
hundred places does not create a hundred token caches; they all resolve the same provider. PayPal tokens
last several hours, so in steady state the whole app makes roughly one token request per token lifetime,
not one per call or per client. Concurrent first-use is collapsed into a single fetch by the semaphore.

(The aggregate `IPayPalApiClient` itself is scoped, which is the right lifetime for a client whose
sub-clients wrap `HttpClientFactory`-managed `HttpClient`s. Its lifetime is independent of the token
cache, which lives in the singleton provider.)

When you build clients with `IPayPalClientFactory` instead, each distinct credential set gets its own
provider and cache, reused across calls, so every merchant caches its own token.

## Getting a raw token

If you need to call something not yet wrapped, inject the provider:

```csharp
public sealed class Raw(IPayPalTokenProvider tokens)
{
    public Task<string> Bearer() => tokens.GetAccessTokenAsync();
}
```

Or read it straight off any client, DI-injected or factory-built: `await client.Tokens.GetAccessTokenAsync()`.

Token acquisition failures throw `PayPalAuthenticationException`.

## Bringing your own token

`IPayPalTokenProvider` is the seam for auth. To use tokens you obtained elsewhere, supply your own
implementation: `StaticPayPalTokenProvider` (a fixed token) or `DelegatePayPalTokenProvider` (your
callback). In DI, register one before `AddPayPalSharp` and it replaces the client-credentials provider;
with the factory, use `CreateWithAccessToken` / `CreateWithTokenProvider`. See
[Multi-tenant](Multi-Tenant-Factory.md).
