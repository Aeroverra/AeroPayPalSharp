# Models and nullability

PayPal marks almost nothing required, so the SDK tightens the generated models while staying crash-proof.

## Money is `decimal`

Money `value` fields are C# `decimal` (not string). On the wire they serialize as a currency-safe string:
trailing zeros are trimmed, so a zero-decimal currency like JPY is never rejected.

```csharp
new Money { CurrencyCode = PayPalCurrency.Usd, Value = 10.00m };   // -> "10"
new Money { CurrencyCode = PayPalCurrency.Jpy, Value = 5000m };    // -> "5000"  (not "5000.00")
new Money { CurrencyCode = PayPalCurrency.Usd, Value = 9.99m };    // -> "9.99"
```

## Enums are strings

PayPal adds enum values constantly; a C# enum would throw on an unseen one. Enums are `string` (allowed
values listed in each property's XML doc). Use the typed [constants](Constants-and-Enums.md) instead of
literals.

## Known fields are non-null

A curated set of always-present response fields (a webhook's `id`, a create-referral response's `links`)
is marked required; everything uncertain stays nullable so an edge case never breaks parsing.

## Naming

Idiomatic PascalCase (`OrderRequest`, `CurrencyCode`, `AmountWithBreakdown`). The wire format is unchanged
(`[JsonProperty("currency_code")]` is kept). A numeric suffix appears only where PayPal genuinely defines
different schemas with the same name, or a property collides with its class (`Operation1`).
