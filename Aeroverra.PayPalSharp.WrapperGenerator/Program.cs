using Aeroverra.PayPalSharp.WrapperGenerator;
using Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer;
using Aeroverra.PayPalSharp.WrapperGenerator.SpecFixer.Transformer;
using Newtonsoft.Json.Linq;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

// Regenerates the AeroPayPalSharp API clients (Aeroverra.PayPalSharp/Generated/*.cs)
// from PayPal's official OpenAPI specs, applying the transformer pipeline that fixes
// PayPal's spec quirks and tightens the models.
//
//   dotnet run --project Aeroverra.PayPalSharp.WrapperGenerator             (generate)
//   dotnet run --project Aeroverra.PayPalSharp.WrapperGenerator -- --download (refresh raw specs first)
//
// The committed Definitions/*.json are the RAW PayPal specs; transformers run in
// memory at generation time so iterating on a transformer is just "run again".

internal static class Program
{
    private sealed record ClientSpec(
        string Name,
        string InputFile,
        string ClassName,
        string Namespace,
        string OutputFile,
        string ResourceKey,
        string? DownloadUrl);

    private const string PayPalSpecBase =
        "https://raw.githubusercontent.com/paypal/paypal-rest-api-specifications/main/openapi/";

    // Milestone 1: mirror the current Vi.Aero.PayPal surface.
    // (Partner Referrals v1 is 404 on PayPal's repo now, so it has no DownloadUrl -
    // its committed copy is sourced from the reference lib.)
    private static readonly ClientSpec[] Clients =
    {
        new("Partner Referrals V2", "customer_partner_referrals_v2.json", "PartnerReferralsV2Client",
            "Aeroverra.PayPalSharp.PartnerReferralsV2", "PartnerReferralsV2Client.cs", "partner-referrals",
            PayPalSpecBase + "customer_partner_referrals_v2.json"),
        new("Partner Referrals V1", "customer_partner_referrals_v1.json", "PartnerReferralsV1Client",
            "Aeroverra.PayPalSharp.PartnerReferralsV1", "PartnerReferralsV1Client.cs", "partner-referrals",
            null),
        new("Webhooks V1", "notifications_webhooks_v1.json", "WebhooksV1Client",
            "Aeroverra.PayPalSharp.WebhooksV1", "WebhooksV1Client.cs", "webhooks",
            PayPalSpecBase + "notifications_webhooks_v1.json"),

        new("Orders V2", "checkout_orders_v2.json", "OrdersV2Client",
            "Aeroverra.PayPalSharp.OrdersV2", "OrdersV2Client.cs", "orders",
            PayPalSpecBase + "checkout_orders_v2.json"),
        new("Payments V2", "payments_payment_v2.json", "PaymentsV2Client",
            "Aeroverra.PayPalSharp.PaymentsV2", "PaymentsV2Client.cs", "",
            PayPalSpecBase + "payments_payment_v2.json"),
        new("Invoices V2", "invoicing_v2.json", "InvoicesV2Client",
            "Aeroverra.PayPalSharp.InvoicesV2", "InvoicesV2Client.cs", "invoices",
            PayPalSpecBase + "invoicing_v2.json"),
        new("Subscriptions V1", "billing_subscriptions_v1.json", "SubscriptionsV1Client",
            "Aeroverra.PayPalSharp.SubscriptionsV1", "SubscriptionsV1Client.cs", "subscriptions",
            PayPalSpecBase + "billing_subscriptions_v1.json"),
        new("Catalog Products V1", "catalogs_products_v1.json", "CatalogProductsV1Client",
            "Aeroverra.PayPalSharp.CatalogProductsV1", "CatalogProductsV1Client.cs", "products",
            PayPalSpecBase + "catalogs_products_v1.json"),
        new("Disputes V1", "customer_disputes_v1.json", "DisputesV1Client",
            "Aeroverra.PayPalSharp.DisputesV1", "DisputesV1Client.cs", "disputes",
            PayPalSpecBase + "customer_disputes_v1.json"),
        new("Payouts V1", "payments_payouts_batch_v1.json", "PayoutsV1Client",
            "Aeroverra.PayPalSharp.PayoutsV1", "PayoutsV1Client.cs", "payouts",
            PayPalSpecBase + "payments_payouts_batch_v1.json"),
        new("Transaction Search V1", "reporting_transactions_v1.json", "TransactionSearchV1Client",
            "Aeroverra.PayPalSharp.TransactionSearchV1", "TransactionSearchV1Client.cs", "",
            PayPalSpecBase + "reporting_transactions_v1.json"),
        new("Shipment Tracking V1", "shipping_shipment_tracking_v1.json", "ShipmentTrackingV1Client",
            "Aeroverra.PayPalSharp.ShipmentTrackingV1", "ShipmentTrackingV1Client.cs", "trackers",
            PayPalSpecBase + "shipping_shipment_tracking_v1.json"),
        new("Payment Tokens V3", "vault_payment_tokens_v3.json", "PaymentTokensV3Client",
            "Aeroverra.PayPalSharp.PaymentTokensV3", "PaymentTokensV3Client.cs", "payment-tokens",
            PayPalSpecBase + "vault_payment_tokens_v3.json"),
        new("Web Profiles V1", "payment-experience_web_experience_profiles_v1.json", "WebProfilesV1Client",
            "Aeroverra.PayPalSharp.WebProfilesV1", "WebProfilesV1Client.cs", "web-profile",
            PayPalSpecBase + "payment-experience_web_experience_profiles_v1.json"),

        // Hand-maintained supplement (no DownloadUrl -> never auto-updates). Home for endpoints and
        // models PayPal does not publish in its official specs, e.g. the webhook cert endpoint.
        new("Custom", "paypal_custom_v1.json", "PayPalCustomClient",
            "Aeroverra.PayPalSharp.CustomV1", "PayPalCustomClient.cs", "",
            null),
    };

    // The transformer pipeline applied to every spec, in order.
    private static ITransformer[] Transformers() => new ITransformer[]
    {
        new StripCallbacks(),
        new EnsureOperationResponses(),
        new FlattenEnumsToString(),
        new InlineStringAllOf(),
        new MarkKnownRequired(),
    };

    private static async Task<int> Main(string[] args)
    {
        try
        {
            var solutionRoot = FindSolutionRoot();
            var definitionsDir = Path.Combine(solutionRoot, "Aeroverra.PayPalSharp.WrapperGenerator", "Definitions");
            var generatedDir = Path.Combine(solutionRoot, "Aeroverra.PayPalSharp", "Generated");

            Console.WriteLine($"Solution root : {solutionRoot}");
            Console.WriteLine($"Definitions   : {definitionsDir}");
            Console.WriteLine($"Generated     : {generatedDir}");

            if (args.Any(a => string.Equals(a, "--download", StringComparison.OrdinalIgnoreCase)))
            {
                await DownloadSpecsAsync(definitionsDir);
            }

            Directory.CreateDirectory(generatedDir);
            GenerateClients(definitionsDir, generatedDir);

            Console.WriteLine("PayPal API client generation completed successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex}");
            return 1;
        }
    }

    private static void GenerateClients(string definitionsDir, string generatedDir)
    {
        Console.WriteLine("Generating C# API clients ...");
        foreach (var client in Clients)
        {
            var inputPath = Path.Combine(definitionsDir, client.InputFile);
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"OpenAPI definition not found: {inputPath}");
            }

            Console.WriteLine($" - {client.Name}: {client.InputFile}");

            // Load raw spec, run the transformer pipeline in memory, then hand to NSwag.
            var spec = JObject.Parse(File.ReadAllText(inputPath));
            foreach (var transformer in Transformers())
            {
                transformer.Transform(spec);
            }

            var document = OpenApiDocument.FromJsonAsync(spec.ToString()).GetAwaiter().GetResult();
            var generator = new CSharpClientGenerator(document, BuildSettings(client));
            var code = generator.GenerateFile();

            // NSwag emits LF; normalize to CRLF for consistency on Windows.
            code = code.Replace("\r\n", "\n").Replace("\n", "\r\n");

            var outputPath = Path.Combine(generatedDir, client.OutputFile);
            File.WriteAllText(outputPath, code);
            Console.WriteLine($"   wrote {client.OutputFile}");
        }
    }

    private static CSharpClientGeneratorSettings BuildSettings(ClientSpec client)
    {
        return new CSharpClientGeneratorSettings
        {
            ClassName = client.ClassName,
            GenerateClientClasses = true,
            GenerateClientInterfaces = true,
            InjectHttpClient = true,
            DisposeHttpClient = false,
            // One shared PayPalApiException lives in the root namespace (Exceptions/PayPalApiException.cs)
            // so a single catch handles errors from every sub-client. Do not generate it per client.
            GenerateExceptionClasses = false,
            ExceptionClass = "PayPalApiException",
            UseBaseUrl = false,
            GenerateBaseUrlProperty = false,
            GenerateSyncMethods = false,
            ClientClassAccessModifier = "public",
            ParameterDateTimeFormat = "s",
            ParameterDateFormat = "yyyy-MM-dd",
            GenerateUpdateJsonSerializerSettingsMethod = true,
            OperationNameGenerator = new PayPalOperationNameGenerator(client.ResourceKey),
            GenerateOptionalParameters = true,
            ParameterArrayType = "System.Collections.Generic.IEnumerable",
            ParameterDictionaryType = "System.Collections.Generic.IDictionary",
            ResponseArrayType = "System.Collections.Generic.ICollection",
            ResponseDictionaryType = "System.Collections.Generic.IDictionary",
            WrapResponses = false,
            GenerateResponseClasses = true,
            ResponseClass = "SwaggerResponse",
            GenerateDtoTypes = true,
            CSharpGeneratorSettings =
            {
                Namespace = client.Namespace,
                TypeAccessModifier = "public",
                RequiredPropertiesMustBeDefined = true,
                DateType = "System.DateTimeOffset",
                AnyType = "object",
                DateTimeType = "System.DateTimeOffset",
                TimeType = "System.TimeSpan",
                TimeSpanType = "System.TimeSpan",
                ArrayType = "System.Collections.Generic.ICollection",
                ArrayInstanceType = "System.Collections.ObjectModel.Collection",
                DictionaryType = "System.Collections.Generic.IDictionary",
                DictionaryInstanceType = "System.Collections.Generic.Dictionary",
                ArrayBaseType = "System.Collections.ObjectModel.Collection",
                DictionaryBaseType = "System.Collections.Generic.Dictionary",
                ClassStyle = CSharpClassStyle.Poco,
                JsonLibrary = CSharpJsonLibrary.NewtonsoftJson,
                GenerateDefaultValues = true,
                GenerateDataAnnotations = true,
                HandleReferences = false,
                InlineNamedArrays = false,
                InlineNamedDictionaries = false,
                InlineNamedTuples = true,
                InlineNamedAny = false,
                GenerateJsonMethods = false,
                EnforceFlagEnums = false,
                GenerateOptionalPropertiesAsNullable = false,
                GenerateNullableReferenceTypes = true,
            },
        };
    }

    private static async Task DownloadSpecsAsync(string definitionsDir)
    {
        Directory.CreateDirectory(definitionsDir);
        using var http = new HttpClient();
        foreach (var client in Clients.Where(c => c.DownloadUrl is not null))
        {
            Console.WriteLine($"Downloading {client.Name} from {client.DownloadUrl}");
            var json = await http.GetStringAsync(client.DownloadUrl);
            await File.WriteAllTextAsync(Path.Combine(definitionsDir, client.InputFile), json);
        }
    }

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("Aeroverra.PayPalSharp.slnx").Length > 0)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate Aeroverra.PayPalSharp.slnx above '{AppContext.BaseDirectory}'.");
    }
}
