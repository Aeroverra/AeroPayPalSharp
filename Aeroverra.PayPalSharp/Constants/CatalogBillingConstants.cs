namespace Aeroverra.PayPalSharp;

/// <summary>Catalog product type (<c>type</c>).</summary>
public static class PayPalProductType
{
    public const string Physical = "PHYSICAL";
    public const string Digital = "DIGITAL";
    public const string Service = "SERVICE";

    public static readonly IReadOnlyList<string> All = new[] { Physical, Digital, Service };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>Subscription status.</summary>
public static class PayPalSubscriptionStatus
{
    public const string ApprovalPending = "APPROVAL_PENDING";
    public const string Approved = "APPROVED";
    public const string Active = "ACTIVE";
    public const string Suspended = "SUSPENDED";
    public const string Cancelled = "CANCELLED";
    public const string Expired = "EXPIRED";

    public static readonly IReadOnlyList<string> All = new[] { ApprovalPending, Approved, Active, Suspended, Cancelled, Expired };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}
