using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// For clients whose reads need a pre-existing resource (a real authorization, payout
/// batch, or vaulted token), a lookup of an unknown id still proves the whole path works:
/// auth is attached, the URL routes, and PayPal's error deserializes into the typed
/// PayPalApiException. Each asserts a 4xx (not a transport/500).
/// </summary>
[Collection(PayPalCollection.Name)]
public class WiringTests
{
    private readonly PayPalTestFixture _fx;

    public WiringTests(PayPalTestFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Payments_get_unknown_authorization_is_client_error()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);
        var ex = await Assert.ThrowsAnyAsync<Aeroverra.PayPalSharp.PaymentsV2.PayPalApiException>(
            () => _fx.Client.Payments.AuthorizationsGetAsync("AUTH-DOES-NOT-EXIST"));
        Assert.InRange(ex.StatusCode, 400, 499);
    }

    [SkippableFact]
    public async Task Payouts_get_unknown_batch_is_client_error()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);
        var ex = await Assert.ThrowsAnyAsync<Aeroverra.PayPalSharp.PayoutsV1.PayPalApiException>(
            () => _fx.Client.Payouts.GetAsync("BATCH-DOES-NOT-EXIST"));
        Assert.InRange(ex.StatusCode, 400, 499);
    }

    [SkippableFact]
    public async Task PaymentTokens_get_unknown_is_client_error()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);
        var ex = await Assert.ThrowsAnyAsync<Aeroverra.PayPalSharp.PaymentTokensV3.PayPalApiException>(
            () => _fx.Client.PaymentTokens.GetAsync("TOKEN-DOES-NOT-EXIST"));
        Assert.InRange(ex.StatusCode, 400, 499);
    }
}
