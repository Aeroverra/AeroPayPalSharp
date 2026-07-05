# Partner and platform (multiparty)

Two partner headers are added automatically by `PayPalPartnerHeaderHandler`:

- **`PayPal-Partner-Attribution-Id`** (your BN code) is sent on every request when `PartnerAttributionId` is set.
- **`PayPal-Auth-Assertion`** makes a call run on behalf of a sub-merchant.

## Acting on behalf of a seller

Two separate things, and it is easy to conflate them:

- **Who receives the money** is the order's `payee.merchant_id`, in the request **body** (not a header).
- **Acting as the seller** (using the permissions they granted your platform) is the
  `PayPal-Auth-Assertion` **header**.

The cleanest way to send that header is a scope on the client. Everything inside the `using` runs on
behalf of that merchant, and the value flows across awaits and is restored on dispose, so a single
injected client serves many sellers safely (including concurrently):

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
                Amount = new Amount3 { Currency_code = "USD", Value = "10.00" },
                Payee  = new Payee3 { Merchant_id = sellerMerchantId },   // who gets the money
            },
        },
    };

    Order created = await paypal.Orders.CreateAsync(order, payPal_Request_Id: Guid.NewGuid().ToString("N"));
}
```

## Other ways to set the assertion

- **Globally**, for a client dedicated to one seller: set `SendAuthAssertion = true` + `MerchantId` in
  options (or on `PayPalCredentials` when building via the factory). Every call then acts as that merchant.
- **Explicitly per call**, where PayPal exposes it: many methods take a `payPal_Auth_Assertion` argument.
  Build the value with `PayPalAuthAssertion.Build(clientId, merchantId)` if you want to pass it yourself.

Per-call headers you set yourself always win; the handler only fills in what you did not.

Many create/capture methods also accept a `payPal_Request_Id` (idempotency) argument and a `prefer`
argument (pass `"return=representation"` for the full object in the response).
