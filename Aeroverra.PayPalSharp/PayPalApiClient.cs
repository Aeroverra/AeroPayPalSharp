using Aeroverra.PayPalSharp.CatalogProductsV1;
using Aeroverra.PayPalSharp.CustomV1;
using Aeroverra.PayPalSharp.DisputesV1;
using Aeroverra.PayPalSharp.InvoicesV2;
using Aeroverra.PayPalSharp.OrdersV2;
using Aeroverra.PayPalSharp.PartnerReferralsV1;
using Aeroverra.PayPalSharp.PartnerReferralsV2;
using Aeroverra.PayPalSharp.PaymentsV2;
using Aeroverra.PayPalSharp.PaymentTokensV3;
using Aeroverra.PayPalSharp.PayoutsV1;
using Aeroverra.PayPalSharp.ShipmentTrackingV1;
using Aeroverra.PayPalSharp.SubscriptionsV1;
using Aeroverra.PayPalSharp.TransactionSearchV1;
using Aeroverra.PayPalSharp.WebhooksV1;
using Aeroverra.PayPalSharp.WebProfilesV1;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// One PayPal client to inject. Exposes each PayPal API as a strongly-typed sub-client
/// (for example <c>client.Orders.CreateAsync(...)</c>, <c>client.Webhooks.ListAsync()</c>).
/// Use <see cref="ActingAsMerchant"/> to run a block of calls on behalf of a sub-merchant.
/// </summary>
public interface IPayPalApiClient
{
    /// <summary>Orders v2 - create, authorize, capture, and manage checkout orders.</summary>
    IOrdersV2Client Orders { get; }

    /// <summary>Payments v2 - authorizations, captures, refunds.</summary>
    IPaymentsV2Client Payments { get; }

    /// <summary>Invoicing v2 - create, send, and manage invoices and templates.</summary>
    IInvoicesV2Client Invoices { get; }

    /// <summary>Subscriptions v1 (billing) - plans and subscriptions.</summary>
    ISubscriptionsV1Client Subscriptions { get; }

    /// <summary>Catalog Products v1 - the product catalog used by subscriptions.</summary>
    ICatalogProductsV1Client CatalogProducts { get; }

    /// <summary>Disputes v1 - customer disputes and evidence.</summary>
    IDisputesV1Client Disputes { get; }

    /// <summary>Payouts v1 - batch payouts and payout items.</summary>
    IPayoutsV1Client Payouts { get; }

    /// <summary>Transaction Search v1 - transaction and balance reporting.</summary>
    ITransactionSearchV1Client TransactionSearch { get; }

    /// <summary>Shipment Tracking v1 - add and update tracking for transactions.</summary>
    IShipmentTrackingV1Client ShipmentTracking { get; }

    /// <summary>Payment Method Tokens v3 - vaulted payment methods and setup tokens.</summary>
    IPaymentTokensV3Client PaymentTokens { get; }

    /// <summary>Payment Experience v1 - hosted web experience profiles.</summary>
    IWebProfilesV1Client WebProfiles { get; }

    /// <summary>Partner Referrals v2 - onboard sellers to PayPal Complete Payments.</summary>
    IPartnerReferralsV2Client PartnerReferralsV2 { get; }

    /// <summary>Partner Referrals v1 - legacy seller onboarding (deprecated).</summary>
    IPartnerReferralsV1Client PartnerReferralsV1 { get; }

    /// <summary>Webhooks management - subscriptions, event types, lookups, simulation.</summary>
    IWebhooksV1Client Webhooks { get; }

    /// <summary>Hand-maintained endpoints PayPal does not publish in its specs (e.g. the webhook cert endpoint).</summary>
    IPayPalCustomClient Custom { get; }

    /// <summary>
    /// Runs the calls inside the returned scope on behalf of <paramref name="merchantId"/> by
    /// attaching a <c>PayPal-Auth-Assertion</c> for that sub-merchant. Usage:
    /// <code>using (client.ActingAsMerchant(sellerMerchantId)) { await client.Orders.CreateAsync(order); }</code>
    /// </summary>
    IDisposable ActingAsMerchant(string merchantId);
}

/// <inheritdoc />
public sealed class PayPalApiClient : IPayPalApiClient
{
    private readonly PayPalMerchantContext _merchantContext;

    public PayPalApiClient(
        IOrdersV2Client orders,
        IPaymentsV2Client payments,
        IInvoicesV2Client invoices,
        ISubscriptionsV1Client subscriptions,
        ICatalogProductsV1Client catalogProducts,
        IDisputesV1Client disputes,
        IPayoutsV1Client payouts,
        ITransactionSearchV1Client transactionSearch,
        IShipmentTrackingV1Client shipmentTracking,
        IPaymentTokensV3Client paymentTokens,
        IWebProfilesV1Client webProfiles,
        IPartnerReferralsV2Client partnerReferralsV2,
        IPartnerReferralsV1Client partnerReferralsV1,
        IWebhooksV1Client webhooks,
        IPayPalCustomClient custom,
        PayPalMerchantContext merchantContext)
    {
        Orders = orders;
        Payments = payments;
        Invoices = invoices;
        Subscriptions = subscriptions;
        CatalogProducts = catalogProducts;
        Disputes = disputes;
        Payouts = payouts;
        TransactionSearch = transactionSearch;
        ShipmentTracking = shipmentTracking;
        PaymentTokens = paymentTokens;
        WebProfiles = webProfiles;
        PartnerReferralsV2 = partnerReferralsV2;
        PartnerReferralsV1 = partnerReferralsV1;
        Webhooks = webhooks;
        Custom = custom;
        _merchantContext = merchantContext;
    }

    public IOrdersV2Client Orders { get; }
    public IPaymentsV2Client Payments { get; }
    public IInvoicesV2Client Invoices { get; }
    public ISubscriptionsV1Client Subscriptions { get; }
    public ICatalogProductsV1Client CatalogProducts { get; }
    public IDisputesV1Client Disputes { get; }
    public IPayoutsV1Client Payouts { get; }
    public ITransactionSearchV1Client TransactionSearch { get; }
    public IShipmentTrackingV1Client ShipmentTracking { get; }
    public IPaymentTokensV3Client PaymentTokens { get; }
    public IWebProfilesV1Client WebProfiles { get; }
    public IPartnerReferralsV2Client PartnerReferralsV2 { get; }
    public IPartnerReferralsV1Client PartnerReferralsV1 { get; }
    public IWebhooksV1Client Webhooks { get; }
    public IPayPalCustomClient Custom { get; }

    public IDisposable ActingAsMerchant(string merchantId) => _merchantContext.ActingAs(merchantId);
}
