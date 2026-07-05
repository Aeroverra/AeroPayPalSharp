# AeroPayPalSharp

A strict-typed PayPal REST client for modern .NET, with first-class partner / platform (multiparty) support.

## Why

The existing .NET options for PayPal are dated and loosely typed, and none cleanly support the partner
APIs (acting on behalf of sellers). AeroPayPalSharp is generated from PayPal's official OpenAPI specs
and then tightened so responses are actually typed instead of everything-nullable. It wraps all of
PayPal's REST APIs behind one injectable client, handles OAuth for you, and makes partner flows (auth
assertion, attribution) first-class.

- All of PayPal's REST APIs, one injectable `IPayPalApiClient`
- Strongly-typed models, plus typed value constants (`PayPalIntent.Capture`, `PayPalCurrency.Usd`, ...)
  with an `IsKnown` validator instead of magic strings
- PayPal's enum fields stay strings under the hood, so a value PayPal adds later never crashes deserialization
- OAuth2 fetched, cached, and refreshed automatically
- Partner attribution and auth assertion built in, including a clean `ActingAsMerchant` scope
- .NET 10, Newtonsoft.Json

## Install

```bash
dotnet add package AeroPayPalSharp
```

## Getting started

Configure and inject (keep the client id / secret in user-secrets or env vars):

```csharp
services.AddPayPalSharp(options =>
{
    options.Environment  = PayPalEnvironment.Sandbox;   // or Live
    options.ClientId     = config["PayPal:ClientId"];
    options.ClientSecret = config["PayPal:ClientSecret"];
    // options.PartnerAttributionId = config["PayPal:PartnerAttributionId"]; // optional: partner/platform BN code
});
```

Create an order and read it back:

```csharp
public sealed class Checkout(IPayPalApiClient paypal)
{
    public async Task<string> CreateOrder()
    {
        var order = new Order_request
        {
            Intent = PayPalIntent.Capture,
            Purchase_units = new List<Purchase_units>
            {
                new Purchase_units { Amount = new Amount3 { Currency_code = PayPalCurrency.Usd, Value = "10.00" } },
            },
        };

        Order created = await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
        Order fetched = await paypal.Orders.GetAsync(created.Id);
        return fetched.Status;   // compare with PayPalOrderStatus.Created, etc.
    }
}
```

Values like `PayPalIntent.Capture` and `PayPalCurrency.Usd` are typed constants (with an `IsKnown`
validator) so you never hand-type a magic string. See [Constants and enums](wiki/Constants-and-Enums.md).

### As a partner, acting for a seller

Direct the funds to the seller with `payee.merchant_id`, and run the call on their behalf with an
`ActingAsMerchant` scope (it adds the `PayPal-Auth-Assertion` header for that merchant):

```csharp
using (paypal.ActingAsMerchant(sellerMerchantId))
{
    var order = new Order_request
    {
        Intent = "CAPTURE",
        Purchase_units = new List<Purchase_units>
        {
            new Purchase_units
            {
                Amount = new Amount3 { Currency_code = PayPalCurrency.Usd, Value = "10.00" },
                Payee  = new Payee3 { Merchant_id = sellerMerchantId },   // who gets the money
            },
        },
    };

    Order created = await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
}
```

## Documentation

Everything else lives in the [wiki](wiki):

- [Configuration](wiki/Configuration.md)
- [Authentication](wiki/Authentication.md)
- [Partner and platform (acting for sellers)](wiki/Partner-and-Platform.md)
- [Multi-tenant: clients from raw credentials](wiki/Multi-Tenant-Factory.md)
- [The clients](wiki/Clients.md)
- [Constants and enums](wiki/Constants-and-Enums.md)
- [Error handling](wiki/Error-Handling.md)
- [Models and nullability](wiki/Models-and-Nullability.md)
- [Regenerating the clients](wiki/Regenerating.md)
- [Testing](wiki/Testing.md)

## License

MIT. See [LICENSE.md](LICENSE.md).
