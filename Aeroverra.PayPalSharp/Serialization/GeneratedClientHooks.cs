// Routes each generated client's `UpdateJsonSerializerSettings` partial into the shared
// PayPalJsonSettings.Apply. Each partial class here must match the generated client's
// namespace + class name exactly. Add one block per new generated client.

namespace Aeroverra.PayPalSharp.OrdersV2
{
    public partial class OrdersV2Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.PaymentsV2
{
    public partial class PaymentsV2Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.InvoicesV2
{
    public partial class InvoicesV2Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.SubscriptionsV1
{
    public partial class SubscriptionsV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.CatalogProductsV1
{
    public partial class CatalogProductsV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.DisputesV1
{
    public partial class DisputesV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.PayoutsV1
{
    public partial class PayoutsV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.TransactionSearchV1
{
    public partial class TransactionSearchV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.ShipmentTrackingV1
{
    public partial class ShipmentTrackingV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.PaymentTokensV3
{
    public partial class PaymentTokensV3Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.WebProfilesV1
{
    public partial class WebProfilesV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.PartnerReferralsV2
{
    public partial class PartnerReferralsV2Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.PartnerReferralsV1
{
    public partial class PartnerReferralsV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.WebhooksV1
{
    public partial class WebhooksV1Client
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}

namespace Aeroverra.PayPalSharp.CustomV1
{
    public partial class PayPalCustomClient
    {
        static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);

        // Capture the raw response body so PayPalApiException.Response carries the JSON error detail.
        partial void Initialize() => ReadResponseAsString = true;
    }
}
