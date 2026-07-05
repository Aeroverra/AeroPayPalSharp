using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Adds PayPal partner/platform headers:
/// <list type="bullet">
/// <item><c>PayPal-Partner-Attribution-Id</c> (the BN code), always, when set.</item>
/// <item><c>PayPal-Auth-Assertion</c> so calls run as a sub-merchant. The merchant is, in order:
/// the ambient <c>ActingAsMerchant(...)</c> scope if one is active, otherwise the configured
/// <see cref="PayPalOptions.MerchantId"/> when <see cref="PayPalOptions.SendAuthAssertion"/> is on.</item>
/// </list>
/// Both are skipped if the caller already set them, so a manually supplied per-request header wins.
/// </summary>
public sealed class PayPalPartnerHeaderHandler : DelegatingHandler
{
    private const string PartnerAttributionHeader = "PayPal-Partner-Attribution-Id";
    private const string AuthAssertionHeader = "PayPal-Auth-Assertion";

    private readonly PayPalOptions _options;
    private readonly PayPalMerchantContext _merchantContext;

    public PayPalPartnerHeaderHandler(IOptions<PayPalOptions> options, PayPalMerchantContext merchantContext)
    {
        _options = options.Value;
        _merchantContext = merchantContext;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.PartnerAttributionId)
            && !request.Headers.Contains(PartnerAttributionHeader))
        {
            request.Headers.TryAddWithoutValidation(PartnerAttributionHeader, _options.PartnerAttributionId);
        }

        // Per-call ActingAsMerchant scope wins; otherwise the globally-configured merchant.
        var merchantId = _merchantContext.CurrentMerchantId
                         ?? (_options.SendAuthAssertion ? _options.MerchantId : null);

        if (!string.IsNullOrWhiteSpace(merchantId)
            && !string.IsNullOrWhiteSpace(_options.ClientId)
            && !request.Headers.Contains(AuthAssertionHeader))
        {
            var assertion = PayPalAuthAssertion.Build(_options.ClientId, merchantId!);
            request.Headers.TryAddWithoutValidation(AuthAssertionHeader, assertion);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
