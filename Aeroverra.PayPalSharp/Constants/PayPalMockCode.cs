namespace Aeroverra.PayPalSharp;

/// <summary>
/// Commonly-used <c>mock_application_codes</c> values for sandbox negative testing, for use with
/// <c>client.WithMockResponse(...)</c>. These are the widely-documented codes; the list is NOT
/// exhaustive and PayPal supports more per endpoint (negative testing is a beta feature), so
/// <c>WithMockResponse</c> also accepts any raw string. Use these to avoid magic strings:
/// <code>using (client.WithMockResponse(PayPalMockCode.InstrumentDeclined)) { ... }</code>
/// </summary>
public static class PayPalMockCode
{
    // Orders v2 (create / authorize / capture)
    /// <summary>The payer's payment instrument (card, bank, wallet) was declined.</summary>
    public const string InstrumentDeclined = "INSTRUMENT_DECLINED";
    /// <summary>The payer cannot pay for this transaction.</summary>
    public const string PayerCannotPay = "PAYER_CANNOT_PAY";
    /// <summary>The payer must take an additional action (for example re-authenticate) to continue.</summary>
    public const string PayerActionRequired = "PAYER_ACTION_REQUIRED";
    /// <summary>The transaction was refused.</summary>
    public const string TransactionRefused = "TRANSACTION_REFUSED";
    /// <summary>The order has not been approved by the payer.</summary>
    public const string OrderNotApproved = "ORDER_NOT_APPROVED";
    /// <summary>The order has already been captured.</summary>
    public const string OrderAlreadyCaptured = "ORDER_ALREADY_CAPTURED";
    /// <summary>The maximum number of payment attempts was exceeded.</summary>
    public const string MaxNumberOfPaymentAttemptsExceeded = "MAX_NUMBER_OF_PAYMENT_ATTEMPTS_EXCEEDED";
    /// <summary>The payee account is restricted.</summary>
    public const string PayeeAccountRestricted = "PAYEE_ACCOUNT_RESTRICTED";
    /// <summary>The payer account is restricted.</summary>
    public const string PayerAccountRestricted = "PAYER_ACCOUNT_RESTRICTED";
    /// <summary>The payer account is locked or closed.</summary>
    public const string PayerAccountLockedOrClosed = "PAYER_ACCOUNT_LOCKED_OR_CLOSED";
    /// <summary>The transaction was declined by risk / fraud filters.</summary>
    public const string DeclinedByRiskFraudFilters = "DECLINED_BY_RISK_FRAUD_FILTERS";
    /// <summary>A compliance rule was violated.</summary>
    public const string ComplianceViolation = "COMPLIANCE_VIOLATION";

    // Payments v2 (authorize / capture / refund)
    /// <summary>The authorization has expired.</summary>
    public const string AuthorizationExpired = "AUTHORIZATION_EXPIRED";
    /// <summary>The payment was denied.</summary>
    public const string PaymentDenied = "PAYMENT_DENIED";
    /// <summary>The account has insufficient funds for this transaction.</summary>
    public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
    /// <summary>The maximum number of captures for this authorization was exceeded.</summary>
    public const string MaxCaptureCountExceeded = "MAX_CAPTURE_COUNT_EXCEEDED";
    /// <summary>The capture currency does not match the authorization currency.</summary>
    public const string AuthCaptureCurrencyMismatch = "AUTH_CAPTURE_CURRENCY_MISMATCH";

    // Cards
    /// <summary>The card has expired.</summary>
    public const string CardExpired = "CARD_EXPIRED";

    // Invoicing
    /// <summary>An invoice with the same id already exists.</summary>
    public const string DuplicateInvoiceId = "DUPLICATE_INVOICE_ID";

    // Generic
    /// <summary>Simulate a 500 from PayPal.</summary>
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";

    public static readonly IReadOnlyList<string> All = new[]
    {
        InstrumentDeclined, PayerCannotPay, PayerActionRequired, TransactionRefused, OrderNotApproved,
        OrderAlreadyCaptured, MaxNumberOfPaymentAttemptsExceeded, PayeeAccountRestricted,
        PayerAccountRestricted, PayerAccountLockedOrClosed, DeclinedByRiskFraudFilters, ComplianceViolation,
        AuthorizationExpired, PaymentDenied, InsufficientFunds, MaxCaptureCountExceeded,
        AuthCaptureCurrencyMismatch, CardExpired, DuplicateInvoiceId, InternalServerError,
    };

    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}
