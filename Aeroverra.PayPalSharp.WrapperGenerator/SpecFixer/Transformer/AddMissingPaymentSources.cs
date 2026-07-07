using Newtonsoft.Json.Linq;

namespace Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;

/// <summary>
/// Adds the payment-source methods PayPal's own Orders v2 spec recognizes in its response validation
/// (the mutually-exclusive <c>payment_source</c> key list) but forgot to define on the REQUEST
/// <c>payment_source</c> object. Without this the request schema only exposes 16 methods, so - like the
/// official SDK - you cannot set alipay, multibanco, pay_upon_invoice, oxxo, and friends.
///
/// <c>pay_upon_invoice</c> gets a fully typed schema (it is richly documented). The other APMs are
/// structurally simple and not individually documented in the spec, so they share a permissive
/// <c>apm_request</c> shape (optional name/country/email + <c>additionalProperties</c>): typed and
/// settable, and anything method-specific flows through the extension data - never a wrong required
/// field that would break a valid request. Only touches the checkout orders spec.
/// </summary>
public sealed class AddMissingPaymentSources : ITransformer
{
    // Simple APMs to wire to the shared apm_request shape: key -> human description.
    private static readonly (string Key, string Description)[] SimpleApms =
    {
        ("alipay", "Alipay, a digital wallet popular in China and Asia."),
        ("bancomatpay", "BANCOMAT Pay, an Italian mobile payment method."),
        ("boletobancario", "Boleto Bancario, a voucher-based payment method in Brazil."),
        ("grabpay", "GrabPay, a digital wallet used across Southeast Asia."),
        ("mbway", "MB WAY, a mobile payment method in Portugal."),
        ("multibanco", "Multibanco, a voucher/reference payment method in Portugal."),
        ("oxxo", "OXXO, a cash voucher payment method in Mexico."),
        ("payu", "PayU, a payment method used across several emerging markets."),
        ("safetypay", "SafetyPay, an online bank-transfer method in Latin America."),
        ("satispay", "Satispay, a mobile payment method in Italy."),
        ("swish", "Swish, a mobile payment method in Sweden."),
        ("verkkopankki", "Verkkopankki (Finnish online banking)."),
        ("wechatpay", "WeChat Pay, a digital wallet popular in China."),
    };

    private int _added;

    public void Transform(JObject openApi)
    {
        _added = 0;
        if (openApi["components"]?["schemas"] is not JObject schemas
            || schemas["payment_source"]?["properties"] is not JObject paymentSourceProps)
        {
            return;
        }

        // Only the checkout-orders payment_source is the create-order APM object. Other specs
        // (subscriptions, vault) reuse the name "payment_source" for something else and lack the shared
        // schemas we reference, so scope to the spec that actually defines them.
        foreach (var required in new[] { "name", "date_no_time", "address_portable", "phone_with_national_required_and_country_code" })
        {
            if (schemas[required] is null)
            {
                return;
            }
        }

        // 1. Shared, permissive APM request shape.
        if (schemas["apm_request"] is null)
        {
            schemas["apm_request"] = new JObject
            {
                ["title"] = "apm_request",
                ["description"] = "Information needed to pay using an alternative payment method (APM). Method-specific fields, if any, can be supplied as additional properties.",
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["name"] = Ref("name", "The name of the account holder associated with this payment method."),
                    ["country_code"] = new JObject { ["type"] = "string", ["description"] = "The two-character ISO 3166-1 country code." },
                    ["email"] = new JObject { ["type"] = "string", ["format"] = "email", ["description"] = "The email address of the account holder, when the method requires it." },
                },
                ["additionalProperties"] = true,
            };
        }

        // 2. Fully typed pay_upon_invoice (Ratepay).
        if (schemas["pay_upon_invoice_request"] is null)
        {
            schemas["pay_upon_invoice_request"] = new JObject
            {
                ["title"] = "pay_upon_invoice_request",
                ["description"] = "Information needed to pay using Pay Upon Invoice, an invoice payment method in Germany (Ratepay).",
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["name"] = Ref("name", "The name of the buyer."),
                    ["email"] = new JObject { ["type"] = "string", ["format"] = "email", ["description"] = "The buyer's email address." },
                    ["birth_date"] = Ref("date_no_time", "The buyer's birth date, in YYYY-MM-DD."),
                    ["phone"] = Ref("phone_with_national_required_and_country_code", "The buyer's phone number."),
                    ["billing_address"] = Ref("address_portable", "The buyer's billing address."),
                    ["experience_context"] = new JObject
                    {
                        ["type"] = "object",
                        ["description"] = "Customizes the payer experience for this payment.",
                        ["properties"] = new JObject
                        {
                            ["brand_name"] = new JObject { ["type"] = "string", ["description"] = "The business name shown to the payer." },
                            ["locale"] = new JObject { ["type"] = "string", ["description"] = "The BCP 47 language tag, for example en-DE." },
                            ["logo_url"] = new JObject { ["type"] = "string", ["format"] = "uri", ["description"] = "The URL of the merchant logo shown on the invoice." },
                            ["customer_service_instructions"] = new JObject
                            {
                                ["type"] = "array",
                                ["description"] = "Customer service instructions shown on the invoice.",
                                ["items"] = new JObject { ["type"] = "string" },
                            },
                        },
                        ["additionalProperties"] = true,
                    },
                },
                ["additionalProperties"] = true,
            };
            AddPaymentSourceKey(paymentSourceProps, "pay_upon_invoice", "pay_upon_invoice_request",
                "Pay Upon Invoice (Ratepay), an invoice payment method in Germany.");
        }

        // 3. Wire every simple APM to the shared shape.
        foreach (var (key, description) in SimpleApms)
        {
            AddPaymentSourceKey(paymentSourceProps, key, "apm_request", description);
        }

        System.Console.WriteLine($"   AddMissingPaymentSources: added {_added} payment-source method(s)");
    }

    private void AddPaymentSourceKey(JObject paymentSourceProps, string key, string schemaName, string description)
    {
        if (paymentSourceProps[key] is not null)
        {
            return; // never overwrite an existing (real) definition
        }
        paymentSourceProps[key] = new JObject
        {
            ["allOf"] = new JArray
            {
                new JObject { ["$ref"] = $"#/components/schemas/{schemaName}" },
                new JObject { ["title"] = key, ["description"] = description },
            },
        };
        _added++;
    }

    private static JObject Ref(string schema, string description) => new()
    {
        ["allOf"] = new JArray
        {
            new JObject { ["$ref"] = $"#/components/schemas/{schema}" },
            new JObject { ["description"] = description },
        },
    };
}
