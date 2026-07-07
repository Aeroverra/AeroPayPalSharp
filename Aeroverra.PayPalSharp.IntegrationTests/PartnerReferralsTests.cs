using Aeroverra.PayPalSharp.PartnerReferralsV2;
using Xunit;
using Xunit.Abstractions;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Live Partner Referrals v2 tests - create a seller onboarding referral (as the
/// partner, using the partner-attribution header) and read it back.
/// </summary>
[Collection(PayPalCollection.Name)]
public class PartnerReferralsTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public PartnerReferralsTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    private static ReferralData MinimalReferral() => new()
    {
        TrackingId = "AERO-" + Guid.NewGuid().ToString("N")[..12],
        Operations = new OperationList
        {
            new Operation
            {
                Operation1 = "API_INTEGRATION",
                ApiIntegrationPreference = new IntegrationDetails
                {
                    RestApiIntegration = new RestApiIntegration
                    {
                        IntegrationMethod = "PAYPAL",
                        IntegrationType = "THIRD_PARTY",
                        ThirdPartyDetails = new ThirdPartyDetails
                        {
                            Features = new RestApiIntegrationRestEndpointFeaturesEnumList { "PAYMENT", "REFUND" },
                        },
                    },
                },
            },
        },
        Products = new ProductList { "EXPRESS_CHECKOUT" },
        LegalConsents = new LegalConsentList
        {
            new LegalConsent { Type = "SHARE_DATA_CONSENT", Granted = true },
        },
    };

    [SkippableFact]
    public async Task Create_returns_action_url_and_reads_back()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        CreateReferralDataResponse created;
        try
        {
            created = await _fx.Client.PartnerReferralsV2.CreateAsync(MinimalReferral());
        }
        catch (PayPalApiException ex)
        {
            // Surface PayPal's validation detail so a body tweak is obvious.
            throw new Xunit.Sdk.XunitException($"PayPal {ex.StatusCode}: {ex.Message}\n{(ex as PayPalApiException<object>)?.Result}");
        }

        Assert.NotNull(created.Links);
        Assert.NotEmpty(created.Links);
        // The onboarding link the seller follows.
        Assert.Contains(created.Links, l => string.Equals(l.Rel, "action_url", StringComparison.OrdinalIgnoreCase));
        _output.WriteLine($"referral links: {string.Join(", ", created.Links.Select(l => l.Rel))}");

        // Read it back via the "self" link's trailing id.
        var self = created.Links.FirstOrDefault(l => string.Equals(l.Rel, "self", StringComparison.OrdinalIgnoreCase));
        Skip.If(self is null, "create response had no self link to read back");

        var referralId = self!.Href.TrimEnd('/').Split('/').Last();
        var read = await _fx.Client.PartnerReferralsV2.ReadAsync(referralId);
        Assert.NotNull(read);
        _output.WriteLine($"read back referral id={referralId}");
    }
}
