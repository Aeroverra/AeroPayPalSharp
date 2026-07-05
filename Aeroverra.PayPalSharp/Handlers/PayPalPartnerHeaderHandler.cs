using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Adds PayPal partner/platform headers when configured:
/// <list type="bullet">
/// <item><c>PayPal-Partner-Attribution-Id</c> (the BN code) - always, when set.</item>
/// <item><c>PayPal-Auth-Assertion</c> - only when <see cref="PayPalOptions.SendAuthAssertion"/>
/// is on and a <see cref="PayPalOptions.MerchantId"/> is set, so calls run as that sub-merchant.</item>
/// </list>
/// Both are skipped if the caller already set them (per-call overrides win), so you can
/// still supply a per-request auth-assertion via <see cref="PayPalAuthAssertion"/>.
/// </summary>
public sealed class PayPalPartnerHeaderHandler : DelegatingHandler
{
    private const string PartnerAttributionHeader = "PayPal-Partner-Attribution-Id";
    private const string AuthAssertionHeader = "PayPal-Auth-Assertion";

    private readonly PayPalOptions _options;

    public PayPalPartnerHeaderHandler(IOptions<PayPalOptions> options) => _options = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.PartnerAttributionId)
            && !request.Headers.Contains(PartnerAttributionHeader))
        {
            request.Headers.TryAddWithoutValidation(PartnerAttributionHeader, _options.PartnerAttributionId);
        }

        if (_options.SendAuthAssertion
            && !string.IsNullOrWhiteSpace(_options.MerchantId)
            && !request.Headers.Contains(AuthAssertionHeader))
        {
            var assertion = PayPalAuthAssertion.Build(_options.ClientId, _options.MerchantId!);
            request.Headers.TryAddWithoutValidation(AuthAssertionHeader, assertion);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
