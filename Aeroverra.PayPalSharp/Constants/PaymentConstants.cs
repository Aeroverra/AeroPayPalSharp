namespace Aeroverra.PayPalSharp;

// Statuses read from Payments v2 responses (authorizations, captures, refunds).

/// <summary>Authorization status.</summary>
public static class PayPalAuthorizationStatus
{
    public const string Created = "CREATED";
    public const string Captured = "CAPTURED";
    public const string Denied = "DENIED";
    public const string PartiallyCaptured = "PARTIALLY_CAPTURED";
    public const string Voided = "VOIDED";
    public const string Pending = "PENDING";

    public static readonly IReadOnlyList<string> All = new[] { Created, Captured, Denied, PartiallyCaptured, Voided, Pending };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>Capture status.</summary>
public static class PayPalCaptureStatus
{
    public const string Completed = "COMPLETED";
    public const string Declined = "DECLINED";
    public const string PartiallyRefunded = "PARTIALLY_REFUNDED";
    public const string Pending = "PENDING";
    public const string Refunded = "REFUNDED";
    public const string Failed = "FAILED";

    public static readonly IReadOnlyList<string> All = new[] { Completed, Declined, PartiallyRefunded, Pending, Refunded, Failed };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>Refund status.</summary>
public static class PayPalRefundStatus
{
    public const string Cancelled = "CANCELLED";
    public const string Failed = "FAILED";
    public const string Pending = "PENDING";
    public const string Completed = "COMPLETED";

    public static readonly IReadOnlyList<string> All = new[] { Cancelled, Failed, Pending, Completed };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}
