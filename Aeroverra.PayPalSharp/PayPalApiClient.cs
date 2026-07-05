using Aeroverra.PayPalSharp.PartnerReferralsV1;
using Aeroverra.PayPalSharp.PartnerReferralsV2;
using Aeroverra.PayPalSharp.WebhooksV1;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// One PayPal client to inject. Exposes each API area as a strongly-typed sub-client
/// (e.g. <c>client.PartnerReferralsV2.CreateAsync(...)</c>, <c>client.Webhooks.ListAsync()</c>).
/// More areas (Orders, Invoices, ...) are added the same way as the SDK grows.
/// </summary>
public interface IPayPalApiClient
{
    /// <summary>Partner Referrals v2 - onboard sellers to PayPal Complete Payments.</summary>
    IPartnerReferralsV2Client PartnerReferralsV2 { get; }

    /// <summary>Partner Referrals v1 - legacy seller onboarding (deprecated).</summary>
    IPartnerReferralsV1Client PartnerReferralsV1 { get; }

    /// <summary>Webhooks management - subscriptions, event types, event lookups, simulation.</summary>
    IWebhooksV1Client Webhooks { get; }
}

/// <inheritdoc />
public sealed class PayPalApiClient : IPayPalApiClient
{
    public PayPalApiClient(
        IPartnerReferralsV2Client partnerReferralsV2,
        IPartnerReferralsV1Client partnerReferralsV1,
        IWebhooksV1Client webhooks)
    {
        PartnerReferralsV2 = partnerReferralsV2;
        PartnerReferralsV1 = partnerReferralsV1;
        Webhooks = webhooks;
    }

    public IPartnerReferralsV2Client PartnerReferralsV2 { get; }
    public IPartnerReferralsV1Client PartnerReferralsV1 { get; }
    public IWebhooksV1Client Webhooks { get; }
}
