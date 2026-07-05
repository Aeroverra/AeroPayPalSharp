// Routes each generated client's `UpdateJsonSerializerSettings` partial into the shared
// PayPalJsonSettings.Apply. Each partial class here must match the generated client's
// namespace + class name exactly. Add one block per new generated client.

namespace Aeroverra.PayPalSharp.PartnerReferralsV2
{
    public partial class PartnerReferralsV2Client
    {
        static partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);
    }
}

namespace Aeroverra.PayPalSharp.PartnerReferralsV1
{
    public partial class PartnerReferralsV1Client
    {
        static partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);
    }
}

namespace Aeroverra.PayPalSharp.WebhooksV1
{
    public partial class WebhooksV1Client
    {
        static partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings)
            => global::Aeroverra.PayPalSharp.PayPalJsonSettings.Apply(settings);
    }
}
