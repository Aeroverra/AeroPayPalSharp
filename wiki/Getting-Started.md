# Getting started

## Install

```bash
dotnet add package Aeroverra.PayPalSharp
```

Targets **.NET 8** and runs on .NET 8, 9, and 10.

## Register and inject

Configure once in your DI container (keep the client id / secret in user-secrets or env vars):

```csharp
using Aeroverra.PayPalSharp;

services.AddPayPalSharp(options =>
{
    options.Environment  = PayPalEnvironment.Sandbox;   // or Live
    options.ClientId     = config["PayPal:ClientId"];
    options.ClientSecret = config["PayPal:ClientSecret"];
    // options.PartnerAttributionId = config["PayPal:PartnerAttributionId"]; // optional partner BN code
});
```

Then inject `IPayPalApiClient` anywhere and reach the resource sub-clients
(`paypal.Orders`, `paypal.Invoices`, `paypal.Webhooks`, and so on).

## Create an order and read it back

```csharp
public sealed class Checkout(IPayPalApiClient paypal)
{
    public async Task<string> CreateOrder()
    {
        var order = new OrderRequest
        {
            Intent = PayPalIntent.Capture,
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest { Amount = new AmountWithBreakdown { CurrencyCode = PayPalCurrency.Usd, Value = 10.00m } },
            },
        };

        Order created = await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
        Order fetched = await paypal.Orders.GetAsync(created.Id);
        return fetched.Status;
    }
}
```

Values like `PayPalIntent.Capture` are typed constants, not magic strings. See
[Constants and enums](Constants-and-Enums.md).

## Create an order for a seller (partner)

Direct the funds to the seller with `payee.merchant_id`, and run the call on their behalf with an
`ActingAsMerchant` scope:

```csharp
using (paypal.ActingAsMerchant(sellerMerchantId))
{
    var order = new OrderRequest
    {
        Intent = PayPalIntent.Capture,
        PurchaseUnits = new List<PurchaseUnitRequest>
        {
            new PurchaseUnitRequest
            {
                Amount = new AmountWithBreakdown { CurrencyCode = PayPalCurrency.Usd, Value = 10.00m },
                Payee  = new Payee { MerchantId = sellerMerchantId },   // who gets the money
            },
        },
    };

    Order created = await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
}
```

More on this in [Partner and platform](Partner-and-Platform.md).

## Next

- [Configuration](Configuration.md) for every option.
- [The clients](Clients.md) for each PayPal API and its methods.
- [Error handling](Error-Handling.md) for how failures surface.
