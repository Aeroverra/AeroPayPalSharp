namespace Aeroverra.PayPalSharp;

/// <summary>
/// Three-letter ISO-4217 currency codes PayPal supports (for a `currency_code` field).
/// Assign a constant, e.g. <c>Currency_code = PayPalCurrency.Usd</c>. <see cref="IsKnown"/>
/// checks membership. Some currencies have per-account or per-country restrictions.
/// </summary>
public static class PayPalCurrency
{
    public const string Aud = "AUD";
    public const string Brl = "BRL";
    public const string Cad = "CAD";
    public const string Chf = "CHF";
    public const string Cny = "CNY";
    public const string Czk = "CZK";
    public const string Dkk = "DKK";
    public const string Eur = "EUR";
    public const string Gbp = "GBP";
    public const string Hkd = "HKD";
    public const string Huf = "HUF";
    public const string Ils = "ILS";
    public const string Jpy = "JPY";
    public const string Mxn = "MXN";
    public const string Myr = "MYR";
    public const string Nok = "NOK";
    public const string Nzd = "NZD";
    public const string Php = "PHP";
    public const string Pln = "PLN";
    public const string Rub = "RUB";
    public const string Sek = "SEK";
    public const string Sgd = "SGD";
    public const string Thb = "THB";
    public const string Twd = "TWD";
    public const string Usd = "USD";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Aud, Brl, Cad, Chf, Cny, Czk, Dkk, Eur, Gbp, Hkd, Huf, Ils, Jpy, Mxn, Myr,
        Nok, Nzd, Php, Pln, Rub, Sek, Sgd, Thb, Twd, Usd,
    };

    public static bool IsKnown(string? value) => WellKnown.IsKnown(All, value);
}
