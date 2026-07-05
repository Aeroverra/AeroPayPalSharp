# Authentication

You never handle tokens yourself. `AddPayPalSharp` wires two pieces:

- **`PayPalTokenProvider`** POSTs `grant_type=client_credentials` to `/v1/oauth2/token` with HTTP Basic
  (`clientId:clientSecret`), caches the access token, and refreshes it about 60 seconds before expiry.
  A semaphore collapses concurrent refreshes into a single request.
- **`PayPalAuthenticationHandler`** is a `DelegatingHandler` that attaches
  `Authorization: Bearer <token>` to every request from every sub-client.

## Getting a raw token

If you need to call something not yet wrapped, inject the provider:

```csharp
public sealed class Raw(IPayPalTokenProvider tokens)
{
    public Task<string> Bearer() => tokens.GetAccessTokenAsync();
}
```

Token acquisition failures throw `PayPalAuthenticationException`.
