# Constants and enums

PayPal's models carry a lot of string-valued fields (intent, currency, status, disbursement
mode, and so on). This library does not force every one of them into a C# enum, on purpose.

## Why the model properties are strings

PayPal reuses the same model types across requests and responses, and it adds new enum values
over time (new payment states, dispute reasons, processor codes). If those fields were C# enums,
a value PayPal returned that the compiled client had not seen would throw during deserialization
and break an otherwise-fine response. Keeping them as strings means new values never break you.
This is the same choice mature payment SDKs make.

## What you get instead

For the values you actually set (and the statuses you compare against), there are typed constant
classes so you never hand-type a magic string, plus an `IsKnown` check to validate one:

```csharp
var order = new Order_request
{
    Intent = PayPalIntent.Capture,                         // "CAPTURE"
    Purchase_units = new List<Purchase_units>
    {
        new Purchase_units
        {
            Amount = new Amount3 { Currency_code = PayPalCurrency.Usd, Value = "10.00" },
        },
    },
};

// reading back
if (created.Status == PayPalOrderStatus.Completed) { ... }

// validating an incoming value
if (!PayPalCurrency.IsKnown(input)) return BadRequest();
```

Each class also exposes `All` (the known values) for building dropdowns or validation.

## Available classes

- `PayPalIntent`, `PayPalOrderStatus`, `PayPalDisbursementMode`, `PayPalPayeePreferred`,
  `PayPalLandingPage`, `PayPalShippingPreference`, `PayPalUserAction`, `PayPalPatchOp`
- `PayPalAuthorizationStatus`, `PayPalCaptureStatus`, `PayPalRefundStatus`
- `PayPalProductType`, `PayPalSubscriptionStatus`
- `PayPalCurrency`

These cover the common fields. The full set of allowed values for any field is also listed in that
property's XML doc comment (copied from PayPal's spec), so IntelliSense shows them inline.

## When a real enum is the right call

If a field is genuinely fixed and only ever sent in requests (never deserialized from a response),
a real C# enum is safe and gives the strongest typing. Those can be opted in per field. Open an
issue or PR for one you rely on.
