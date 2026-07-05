namespace Aeroverra.PayPalSharp;

// Well-known string values for the fields you set when building Orders requests, plus the
// statuses you read back. PayPal's models keep these as plain strings (so a value PayPal
// adds later never breaks deserialization); these classes give you strongly-typed values
// to assign and an IsKnown check to validate one. Assign directly, e.g.
//   Intent = PayPalIntent.Capture

/// <summary>Order intent (<c>intent</c>).</summary>
public static class PayPalIntent
{
    public const string Capture = "CAPTURE";
    public const string Authorize = "AUTHORIZE";

    public static readonly IReadOnlyList<string> All = new[] { Capture, Authorize };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>Order status (<c>status</c>), read from a returned order.</summary>
public static class PayPalOrderStatus
{
    public const string Created = "CREATED";
    public const string Saved = "SAVED";
    public const string Approved = "APPROVED";
    public const string Voided = "VOIDED";
    public const string Completed = "COMPLETED";
    public const string PayerActionRequired = "PAYER_ACTION_REQUIRED";

    public static readonly IReadOnlyList<string> All = new[] { Created, Saved, Approved, Voided, Completed, PayerActionRequired };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>How the funds are disbursed to a sub-merchant (<c>disbursement_mode</c>).</summary>
public static class PayPalDisbursementMode
{
    public const string Instant = "INSTANT";
    public const string Delayed = "DELAYED";

    public static readonly IReadOnlyList<string> All = new[] { Instant, Delayed };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>Payee payment method preference (<c>payee_preferred</c>).</summary>
public static class PayPalPayeePreferred
{
    public const string Unrestricted = "UNRESTRICTED";
    public const string ImmediatePaymentRequired = "IMMEDIATE_PAYMENT_REQUIRED";

    public static readonly IReadOnlyList<string> All = new[] { Unrestricted, ImmediatePaymentRequired };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>Hosted checkout landing page (<c>landing_page</c>).</summary>
public static class PayPalLandingPage
{
    public const string Login = "LOGIN";
    public const string GuestCheckout = "GUEST_CHECKOUT";
    public const string Billing = "BILLING";
    public const string NoPreference = "NO_PREFERENCE";

    public static readonly IReadOnlyList<string> All = new[] { Login, GuestCheckout, Billing, NoPreference };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>Shipping address preference (<c>shipping_preference</c>).</summary>
public static class PayPalShippingPreference
{
    public const string GetFromFile = "GET_FROM_FILE";
    public const string NoShipping = "NO_SHIPPING";
    public const string SetProvidedAddress = "SET_PROVIDED_ADDRESS";

    public static readonly IReadOnlyList<string> All = new[] { GetFromFile, NoShipping, SetProvidedAddress };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>Buyer flow action (<c>user_action</c>).</summary>
public static class PayPalUserAction
{
    public const string Continue = "CONTINUE";
    public const string PayNow = "PAY_NOW";

    public static readonly IReadOnlyList<string> All = new[] { Continue, PayNow };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}

/// <summary>JSON Patch operation (<c>op</c>) for the PATCH endpoints.</summary>
public static class PayPalPatchOp
{
    public const string Add = "add";
    public const string Remove = "remove";
    public const string Replace = "replace";
    public const string Move = "move";
    public const string Copy = "copy";
    public const string Test = "test";

    public static readonly IReadOnlyList<string> All = new[] { Add, Remove, Replace, Move, Copy, Test };
    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}
