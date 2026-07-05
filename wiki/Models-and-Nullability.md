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

## Numbered type names

NSwag deduplicates structurally similar schemas with numeric suffixes, so you will see types like
`Amount3` or `Payee3`, and a property renamed to `Operation1` where it collided with a class name.
These are the same shapes PayPal documents, just disambiguated.
