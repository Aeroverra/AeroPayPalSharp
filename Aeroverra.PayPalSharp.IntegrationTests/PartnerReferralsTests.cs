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

    private static Referral_data MinimalReferral() => new()
    {
        Tracking_id = "AERO-" + Guid.NewGuid().ToString("N")[..12],
        Operations = new Operation_list
        {
            new Operation
            {
                Operation1 = "API_INTEGRATION",
                Api_integration_preference = new Integration_details
                {
                    Rest_api_integration = new Rest_api_integration
                    {
                        Integration_method = "PAYPAL",
                        Integration_type = "THIRD_PARTY",
                        Third_party_details = new Third_party_details
                        {
                            Features = new Rest_api_integration_rest_endpoint_features_enum_list { "PAYMENT", "REFUND" },
                        },
                    },
                },
            },
        },
        Products = new Product_list { "EXPRESS_CHECKOUT" },
        Legal_consents = new Legal_consent_list
        {
            new Legal_consent { Type = "SHARE_DATA_CONSENT", Granted = true },
        },
    };

    [SkippableFact]
    public async Task Create_returns_action_url_and_reads_back()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        Create_referral_data_response created;
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
