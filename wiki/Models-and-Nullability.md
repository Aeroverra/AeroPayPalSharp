# Models and nullability

PayPal's specs mark almost nothing as required, so a naive generator makes every property nullable.
AeroPayPalSharp tightens this while staying crash-proof.

## Enums are strings

PayPal adds new enum values constantly (payment states, dispute reasons, processor codes, and so on).
A generated C# enum would throw on any value it has not seen, breaking deserialization of an otherwise
fine response. So enums become `string`, with the allowed values preserved in each property's XML doc
comment.

## Known fields are non-null

A curated, evidence-based set of always-present response fields (a webhook's `id`, a create-referral
response's `links`, and so on) is marked required so you do not null-check things that are always
there. Request-only and uncertain fields are left nullable so an edge case never blows up parsing. The
set grows conservatively as tests confirm more fields.

## Naming

Types and properties are idiomatic C# PascalCase (`OrderRequest`, `CurrencyCode`, `AmountWithBreakdown`,
`PaymentInstruction`). The JSON wire format is unchanged - every property keeps its
`[JsonProperty("currency_code")]` attribute, so serialization still uses PayPal's snake_case names.

PayPal wraps most shared components in single-ref `allOf` blocks, which NSwag would otherwise turn into
hundreds of empty numbered aliases (`Amount2`, `Amount3`, `Payee2`, ...). The generator collapses those
wrappers so the real component types are used directly - you write `AmountWithBreakdown` and `Payee`, not
`Amount3` and `Payee3`.

A numeric suffix still appears where PayPal genuinely defines two *different* inline schemas with the same
name (for example several distinct `Name`/`Address` shapes across payment sources), or where a property
name would collide with its class (`Operation1`). Those are real, structurally-different types, kept
distinct on purpose.
