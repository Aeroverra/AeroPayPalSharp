# Authentication

You never handle tokens yourself. `AddPayPalSharp` wires two pieces:

- **`PayPalTokenProvider`** POSTs `grant_type=client_credentials` to `/v1/oauth2/token` with HTTP Basic
  (`clientId:clientSecret`), caches the access token, and refreshes it about 60 seconds before expiry.
  A semaphore collapses concurrent refreshes into a single request.
- **`PayPalAuthenticationHandler`** is a `DelegatingHandler` that attaches
  `Authorization: Bearer <token>` to every request from every sub-client.

## Lifetime: one token, shared

The token provider is a **singleton**, so its cache is shared everywhere the client is injected - roughly
one token fetch per token lifetime (several hours), not one per call. Concurrent first-use collapses to a
single fetch. With `IPayPalClientFactory`, each credential set caches its own token.

`IPayPalApiClient` is itself registered as a **singleton**, so you can inject it into a service of any
lifetime (including your own singletons). It rides a shared, pooled transport handler (DNS-safe), and the
`ActingAsMerchant` / `WithMockResponse` scopes are per-async-flow, so one shared client serves concurrent
requests correctly.

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
