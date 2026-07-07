# AeroPayPalSharp wiki

The full documentation for [AeroPayPalSharp](../README.md), a strict-typed PayPal REST client for
modern .NET with first-class partner / platform support.

## Start here

- **[Getting started](Getting-Started.md)** - install, register in DI, create an order, and create an
  order for a seller as a partner.
- **[Configuration](Configuration.md)** - every `PayPalOptions` setting, sandbox vs live, and how to
  supply secrets (user-secrets, env vars, configuration binding).
- **[Authentication](Authentication.md)** - how OAuth2 client-credentials tokens are fetched, cached,
  and refreshed, and how to get a raw token.

## Using the APIs

- **[The clients](Clients.md)** - the full list of PayPal APIs (Orders, Payments, Invoices,
  Subscriptions, Catalog Products, Disputes, Payouts, Transaction Search, Shipment Tracking, Payment
  Tokens, Web Profiles, Partner Referrals, Webhooks), with example calls including webhook creation and
  signature verification.
- **[Webhooks](Webhooks.md)** - managing subscriptions, plus receiving and verifying incoming webhooks
  offline (no extra API call), with typed event-name constants.
- **[Constants and enums](Constants-and-Enums.md)** - typed value constants (`PayPalIntent.Capture`,
  `PayPalCurrency.Usd`, statuses, ...) with an `IsKnown` validator, and why the model fields are strings.
- **[Error handling](Error-Handling.md)** - the typed `PayPalApiException`, status codes, and the parsed
  error body.
- **[Resilience and observability](Resilience-and-Observability.md)** - safe automatic retries, reading
  `PayPal-Debug-Id`, request/response callbacks, and logging.

## Partner and platform

- **[Partner and platform (acting for sellers)](Partner-and-Platform.md)** - `payee.merchant_id` vs the
  `PayPal-Auth-Assertion` header, the `ActingAsMerchant` scope, attribution, idempotency, and the
  `prefer` header.
- **[Multi-tenant: clients from raw credentials](Multi-Tenant-Factory.md)** - `IPayPalClientFactory` for
  building a client per merchant from their own API keys, with per-credential caching.

## Under the hood

- **[Models and nullability](Models-and-Nullability.md)** - why enums are strings, how known fields are
  made non-null, and NSwag's numbered type names.
- **[Regenerating the clients](Regenerating.md)** - the code generator, the transformer pipeline, and how
  to add a new PayPal API.
- **[Testing](Testing.md)** - running the live sandbox tests, what is covered, and how credentials are
  supplied without committing them.
- **[Releasing (NuGet)](Releasing.md)** - the publish workflow, how the version auto-increments, and
  trusted publishing.

## Quick answers

- **Which PayPal APIs are supported?** All of them. See [The clients](Clients.md).
- **How do I verify an incoming webhook?** [Webhooks](Webhooks.md) (offline, no extra API call).
- **How do I act on behalf of a seller when I am a partner?** [Partner and platform](Partner-and-Platform.md).
- **How do I handle many merchants with their own keys?** [Multi-tenant factory](Multi-Tenant-Factory.md).
- **Are calls retried? Can they double-charge?** No - retries are idempotency-safe. [Resilience](Resilience-and-Observability.md).
- **How do I get the PayPal-Debug-Id for support?** [Resilience](Resilience-and-Observability.md) / [Error handling](Error-Handling.md).
- **How do I set a proxy / custom transport?** `PrimaryHandlerFactory` in [Configuration](Configuration.md).
- **How do I force a sandbox error (negative testing)?** `WithMockResponse(...)` in [Testing](Testing.md).
- **How is money typed?** `decimal`, serialized as a currency-safe string (JPY-aware). [Models](Models-and-Nullability.md).
- **Why is `status` a string and not an enum?** [Constants and enums](Constants-and-Enums.md).
- **How do I regenerate after PayPal updates a spec?** [Regenerating the clients](Regenerating.md).
- **How do I publish a new version to NuGet?** [Releasing](Releasing.md).
