namespace Aeroverra.PayPalSharp;

// Shared membership check for the well-known value classes below.
internal static class WellKnown
{
    public static bool IsKnown(IReadOnlyList<string> all, string? value)
        => value is not null && all.Contains(value, StringComparer.Ordinal);
}
