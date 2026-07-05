namespace Aeroverra.PayPalSharp;

/// <summary>
/// Every PayPal webhook event name (a webhook event's event_type), from PayPal's published
/// event-name reference. Use these instead of magic strings when subscribing to or switching
/// on events, for example
/// <c>if (evt.EventType == PayPalWebhookEventType.PaymentCaptureCompleted)</c>. PayPal may add
/// new names over time, so treat an unknown value as valid; <see cref="IsKnown"/> just reports
/// whether this build recognizes it.
/// </summary>
public static class PayPalWebhookEventType
{
    public const string PaymentAuthorizationCreated = "PAYMENT.AUTHORIZATION.CREATED";
    public const string PaymentAuthorizationVoided = "PAYMENT.AUTHORIZATION.VOIDED";
    public const string PaymentCaptureDeclined = "PAYMENT.CAPTURE.DECLINED";
    public const string PaymentCaptureCompleted = "PAYMENT.CAPTURE.COMPLETED";
    public const string PaymentCapturePending = "PAYMENT.CAPTURE.PENDING";
    public const string PaymentCaptureRefunded = "PAYMENT.CAPTURE.REFUNDED";
    public const string PaymentCaptureReversed = "PAYMENT.CAPTURE.REVERSED";
    public const string PaymentCaptureDenied = "PAYMENT.CAPTURE.DENIED";
    public const string PaymentRefundPending = "PAYMENT.REFUND.PENDING";
    public const string PaymentRefundFailed = "PAYMENT.REFUND.FAILED";
    public const string PaymentPayoutsbatchDenied = "PAYMENT.PAYOUTSBATCH.DENIED";
    public const string PaymentPayoutsbatchProcessing = "PAYMENT.PAYOUTSBATCH.PROCESSING";
    public const string PaymentPayoutsbatchSuccess = "PAYMENT.PAYOUTSBATCH.SUCCESS";
    public const string PaymentPayoutsItemBlocked = "PAYMENT.PAYOUTS-ITEM.BLOCKED";
    public const string PaymentPayoutsItemCanceled = "PAYMENT.PAYOUTS-ITEM.CANCELED";
    public const string PaymentPayoutsItemFailed = "PAYMENT.PAYOUTS-ITEM.FAILED";
    public const string PaymentPayoutsItemHeld = "PAYMENT.PAYOUTS-ITEM.HELD";
    public const string PaymentPayoutsItemRefunded = "PAYMENT.PAYOUTS-ITEM.REFUNDED";
    public const string PaymentPayoutsItemReturned = "PAYMENT.PAYOUTS-ITEM.RETURNED";
    public const string PaymentPayoutsItemSucceeded = "PAYMENT.PAYOUTS-ITEM.SUCCEEDED";
    public const string PaymentPayoutsItemUnclaimed = "PAYMENT.PAYOUTS-ITEM.UNCLAIMED";
    public const string BillingPlanCreated = "BILLING.PLAN.CREATED";
    public const string BillingPlanUpdated = "BILLING.PLAN.UPDATED";
    public const string BillingPlanActivated = "BILLING.PLAN.ACTIVATED";
    public const string BillingPlanPricingChangeActivated = "BILLING.PLAN.PRICING-CHANGE.ACTIVATED";
    public const string BillingPlanDeactivated = "BILLING.PLAN.DEACTIVATED";
    public const string BillingSubscriptionCreated = "BILLING.SUBSCRIPTION.CREATED";
    public const string BillingSubscriptionActivated = "BILLING.SUBSCRIPTION.ACTIVATED";
    public const string BillingSubscriptionUpdated = "BILLING.SUBSCRIPTION.UPDATED";
    public const string BillingSubscriptionExpired = "BILLING.SUBSCRIPTION.EXPIRED";
    public const string BillingSubscriptionCancelled = "BILLING.SUBSCRIPTION.CANCELLED";
    public const string BillingSubscriptionSuspended = "BILLING.SUBSCRIPTION.SUSPENDED";
    public const string BillingSubscriptionReActivated = "BILLING.SUBSCRIPTION.RE-ACTIVATED";
    public const string BillingSubscriptionPaymentFailed = "BILLING.SUBSCRIPTION.PAYMENT.FAILED";
    public const string IdentityAuthorizationConsentRevoked = "IDENTITY.AUTHORIZATION-CONSENT.REVOKED";
    public const string PaymentsPaymentCreated = "PAYMENTS.PAYMENT.CREATED";
    public const string CheckoutOrderApproved = "CHECKOUT.ORDER.APPROVED";
    public const string CheckoutOrderCompleted = "CHECKOUT.ORDER.COMPLETED";
    public const string CheckoutOrderProcessed = "CHECKOUT.ORDER.PROCESSED";
    public const string CheckoutCheckoutBuyerApproved = "CHECKOUT.CHECKOUT.BUYER-APPROVED";
    public const string CheckoutPaymentApprovalReversed = "CHECKOUT.PAYMENT-APPROVAL.REVERSED";
    public const string CustomerDisputeCreated = "CUSTOMER.DISPUTE.CREATED";
    public const string CustomerDisputeResolved = "CUSTOMER.DISPUTE.RESOLVED";
    public const string CustomerDisputeUpdated = "CUSTOMER.DISPUTE.UPDATED";
    public const string RiskDisputeCreated = "RISK.DISPUTE.CREATED";
    public const string InvoicingInvoiceCancelled = "INVOICING.INVOICE.CANCELLED";
    public const string InvoicingInvoiceCreated = "INVOICING.INVOICE.CREATED";
    public const string InvoicingInvoicePaid = "INVOICING.INVOICE.PAID";
    public const string InvoicingInvoiceRefunded = "INVOICING.INVOICE.REFUNDED";
    public const string InvoicingInvoiceScheduled = "INVOICING.INVOICE.SCHEDULED";
    public const string InvoicingInvoiceUpdated = "INVOICING.INVOICE.UPDATED";
    public const string CustomerAccountLimitationAdded = "CUSTOMER.ACCOUNT-LIMITATION.ADDED";
    public const string CustomerAccountLimitationEscalated = "CUSTOMER.ACCOUNT-LIMITATION.ESCALATED";
    public const string CustomerAccountLimitationLifted = "CUSTOMER.ACCOUNT-LIMITATION.LIFTED";
    public const string CustomerAccountLimitationUpdated = "CUSTOMER.ACCOUNT-LIMITATION.UPDATED";
    public const string CustomerMerchantIntegrationCapabilityUpdated = "CUSTOMER.MERCHANT-INTEGRATION.CAPABILITY-UPDATED";
    public const string CustomerMerchantIntegrationProductSubscriptionUpdated = "CUSTOMER.MERCHANT-INTEGRATION.PRODUCT-SUBSCRIPTION-UPDATED";
    public const string CustomerMerchantIntegrationSellerAlreadyIntegrated = "CUSTOMER.MERCHANT-INTEGRATION.SELLER-ALREADY-INTEGRATED";
    public const string CustomerMerchantIntegrationSellerOnboardingInitiated = "CUSTOMER.MERCHANT-INTEGRATION.SELLER-ONBOARDING-INITIATED";
    public const string CustomerMerchantIntegrationSellerConsentGranted = "CUSTOMER.MERCHANT-INTEGRATION.SELLER-CONSENT-GRANTED";
    public const string CustomerMerchantIntegrationSellerEmailConfirmed = "CUSTOMER.MERCHANT-INTEGRATION.SELLER-EMAIL-CONFIRMED";
    public const string MerchantOnboardingCompleted = "MERCHANT.ONBOARDING.COMPLETED";
    public const string MerchantPartnerConsentRevoked = "MERCHANT.PARTNER-CONSENT.REVOKED";
    public const string CustomerManagedAccountAccountCreated = "CUSTOMER.MANAGED-ACCOUNT.ACCOUNT-CREATED";
    public const string CustomerManagedAccountCreationFailed = "CUSTOMER.MANAGED-ACCOUNT.CREATION-FAILED";
    public const string CustomerManagedAccountAccountUpdated = "CUSTOMER.MANAGED-ACCOUNT.ACCOUNT-UPDATED";
    public const string CustomerManagedAccountAccountStatusChanged = "CUSTOMER.MANAGED-ACCOUNT.ACCOUNT-STATUS-CHANGED";
    public const string CustomerManagedAccountRiskAssessed = "CUSTOMER.MANAGED-ACCOUNT.RISK-ASSESSED";
    public const string CustomerManagedAccountNegativeBalanceNotified = "CUSTOMER.MANAGED-ACCOUNT.NEGATIVE-BALANCE-NOTIFIED";
    public const string CustomerManagedAccountNegativeBalanceDebitInitiated = "CUSTOMER.MANAGED-ACCOUNT.NEGATIVE-BALANCE-DEBIT-INITIATED";
    public const string PaymentReferencedPayoutItemCompleted = "PAYMENT.REFERENCED-PAYOUT-ITEM.COMPLETED";
    public const string PaymentReferencedPayoutItemFailed = "PAYMENT.REFERENCED-PAYOUT-ITEM.FAILED";
    public const string PaymentOrderCancelled = "PAYMENT.ORDER.CANCELLED";
    public const string PaymentOrderCreated = "PAYMENT.ORDER.CREATED";
    public const string PaymentSaleCompleted = "PAYMENT.SALE.COMPLETED";
    public const string PaymentSaleDenied = "PAYMENT.SALE.DENIED";
    public const string PaymentSalePending = "PAYMENT.SALE.PENDING";
    public const string PaymentSaleRefunded = "PAYMENT.SALE.REFUNDED";
    public const string PaymentSaleReversed = "PAYMENT.SALE.REVERSED";
    public const string CatalogProductCreated = "CATALOG.PRODUCT.CREATED";
    public const string CatalogProductUpdated = "CATALOG.PRODUCT.UPDATED";
    public const string VaultPaymentTokenCreated = "VAULT.PAYMENT-TOKEN.CREATED";
    public const string VaultPaymentTokenDeleted = "VAULT.PAYMENT-TOKEN.DELETED";
    public const string VaultPaymentTokenDeletionInitiated = "VAULT.PAYMENT-TOKEN.DELETION-INITIATED";
    public const string PaymentTokenizationEnrollmentCreated = "PAYMENT-TOKENIZATION.ENROLLMENT.CREATED";
    public const string PaymentTokenizationMetadataChanged = "PAYMENT-TOKENIZATION.METADATA.CHANGED";
    public const string PaymentTokenizationPaymentCredentialCreated = "PAYMENT-TOKENIZATION.PAYMENT-CREDENTIAL.CREATED";
    public const string PaymentTokenizationPaymentCredentialUpdated = "PAYMENT-TOKENIZATION.PAYMENT-CREDENTIAL.UPDATED";
    public const string PaymentTokenizationTokenStatusChanged = "PAYMENT-TOKENIZATION.TOKEN-STATUS.CHANGED";
    public const string PaymentTokenizationTransactionCreated = "PAYMENT-TOKENIZATION.TRANSACTION.CREATED";

    public static readonly IReadOnlyList<string> All = new[]
    {
        PaymentAuthorizationCreated,
        PaymentAuthorizationVoided,
        PaymentCaptureDeclined,
        PaymentCaptureCompleted,
        PaymentCapturePending,
        PaymentCaptureRefunded,
        PaymentCaptureReversed,
        PaymentCaptureDenied,
        PaymentRefundPending,
        PaymentRefundFailed,
        PaymentPayoutsbatchDenied,
        PaymentPayoutsbatchProcessing,
        PaymentPayoutsbatchSuccess,
        PaymentPayoutsItemBlocked,
        PaymentPayoutsItemCanceled,
        PaymentPayoutsItemFailed,
        PaymentPayoutsItemHeld,
        PaymentPayoutsItemRefunded,
        PaymentPayoutsItemReturned,
        PaymentPayoutsItemSucceeded,
        PaymentPayoutsItemUnclaimed,
        BillingPlanCreated,
        BillingPlanUpdated,
        BillingPlanActivated,
        BillingPlanPricingChangeActivated,
        BillingPlanDeactivated,
        BillingSubscriptionCreated,
        BillingSubscriptionActivated,
        BillingSubscriptionUpdated,
        BillingSubscriptionExpired,
        BillingSubscriptionCancelled,
        BillingSubscriptionSuspended,
        BillingSubscriptionReActivated,
        BillingSubscriptionPaymentFailed,
        IdentityAuthorizationConsentRevoked,
        PaymentsPaymentCreated,
        CheckoutOrderApproved,
        CheckoutOrderCompleted,
        CheckoutOrderProcessed,
        CheckoutCheckoutBuyerApproved,
        CheckoutPaymentApprovalReversed,
        CustomerDisputeCreated,
        CustomerDisputeResolved,
        CustomerDisputeUpdated,
        RiskDisputeCreated,
        InvoicingInvoiceCancelled,
        InvoicingInvoiceCreated,
        InvoicingInvoicePaid,
        InvoicingInvoiceRefunded,
        InvoicingInvoiceScheduled,
        InvoicingInvoiceUpdated,
        CustomerAccountLimitationAdded,
        CustomerAccountLimitationEscalated,
        CustomerAccountLimitationLifted,
        CustomerAccountLimitationUpdated,
        CustomerMerchantIntegrationCapabilityUpdated,
        CustomerMerchantIntegrationProductSubscriptionUpdated,
        CustomerMerchantIntegrationSellerAlreadyIntegrated,
        CustomerMerchantIntegrationSellerOnboardingInitiated,
        CustomerMerchantIntegrationSellerConsentGranted,
        CustomerMerchantIntegrationSellerEmailConfirmed,
        MerchantOnboardingCompleted,
        MerchantPartnerConsentRevoked,
        CustomerManagedAccountAccountCreated,
        CustomerManagedAccountCreationFailed,
        CustomerManagedAccountAccountUpdated,
        CustomerManagedAccountAccountStatusChanged,
        CustomerManagedAccountRiskAssessed,
        CustomerManagedAccountNegativeBalanceNotified,
        CustomerManagedAccountNegativeBalanceDebitInitiated,
        PaymentReferencedPayoutItemCompleted,
        PaymentReferencedPayoutItemFailed,
        PaymentOrderCancelled,
        PaymentOrderCreated,
        PaymentSaleCompleted,
        PaymentSaleDenied,
        PaymentSalePending,
        PaymentSaleRefunded,
        PaymentSaleReversed,
        CatalogProductCreated,
        CatalogProductUpdated,
        VaultPaymentTokenCreated,
        VaultPaymentTokenDeleted,
        VaultPaymentTokenDeletionInitiated,
        PaymentTokenizationEnrollmentCreated,
        PaymentTokenizationMetadataChanged,
        PaymentTokenizationPaymentCredentialCreated,
        PaymentTokenizationPaymentCredentialUpdated,
        PaymentTokenizationTokenStatusChanged,
        PaymentTokenizationTransactionCreated,
    };

    public static bool IsKnown(string? value) => value is not null && All.Contains(value, System.StringComparer.Ordinal);
}
