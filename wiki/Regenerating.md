# Regenerating the clients

The clients are produced by `Aeroverra.PayPalSharp.WrapperGenerator`, a build-time console app that is
not shipped (nothing references it, so its NSwag dependencies never reach the package). The raw PayPal
specs live in `WrapperGenerator/Definitions/`; a transformer pipeline runs over each spec in memory
before NSwag sees it.

```bash
# generate from the committed specs
dotnet run --project Aeroverra.PayPalSharp.WrapperGenerator

# refresh the raw specs from PayPal's GitHub first, then generate
dotnet run --project Aeroverra.PayPalSharp.WrapperGenerator -- --download
```

Output goes to `Aeroverra.PayPalSharp/Generated/*.cs`.

## Hand-maintained specs

Most specs in `Definitions/` are downloaded from PayPal and auto-update. `paypal_custom_v1.json` is
different: it has no download URL and never auto-updates. It is the home for endpoints and models PayPal
does not publish in its official specs (today, the webhook signing-certificate endpoint, exposed as
`client.Custom.CertsGetAsync`). Add anything the official specs miss here and it generates like the rest.

## The transformer pipeline

Runs in this order (see `WrapperGenerator/SpecFixer/Transformer/`):

1. **StripCallbacks** removes OpenAPI `callbacks` (async callback path-items). NSwag mishandles PayPal's
   use of them and ends up deserializing a real path-item as an operation ("Required property
   'responses' not found").
2. **EnsureOperationResponses** backfills a default `responses` object on any operation missing one
   (some PayPal operations ship without it, which is invalid OpenAPI).
3. **FlattenEnumsToString** replaces string enums with `string` (see
   [Models and nullability](Models-and-Nullability.md)).
4. **InlineStringAllOf** collapses PayPal's `allOf: [ <string-ref>, <description> ]` wrapper to an inline
   string. Without this, after flattening, NSwag emits `class X : string`, which does not compile.
5. **CollapseSingleRefAllOf** collapses `allOf: [ {$ref}, <cosmetic-only> ]` object wrappers to the bare
   `$ref`, so PayPal's shared components (money, payee, name, ...) stop exploding into hundreds of empty
   numbered alias classes. Only collapses when the siblings are purely cosmetic (description, readOnly).
6. **MarkKnownRequired** marks a curated set of always-present response fields required.
7. **MoneyValueToDecimal** retypes money `value` fields from string to `decimal` (serialized back to a
   currency-safe string by `PayPalMoneyConverter`).
8. **FixMerchantIntegrationFindResponse** gives the partner merchant-integration "find" endpoint its real
   response type (PayPal's spec leaves it untyped).

Type and property names are then PascalCased by generators in `WrapperGenerator/Naming/` (the
`[JsonProperty(...)]` attributes preserve the JSON wire names).

## Adding a new PayPal API

1. Add a `ClientSpec` to the `Clients` array in `WrapperGenerator/Program.cs`
   (name, spec file, class name, namespace, output file, resource key, download URL).
2. Run the generator; a new `Generated/<Name>Client.cs` appears.
3. Add the `UpdateJsonSerializerSettings` hook for the new namespace in
   `Serialization/GeneratedClientHooks.cs`.
4. Expose it on `IPayPalApiClient` / `PayPalApiClient` and register it in
   `ServiceCollectionExtensions.AddApiClient<...>`, and in `PayPalClientFactory.Build`.
5. Add any resource-specific `MarkKnownRequired` entries and tests.
