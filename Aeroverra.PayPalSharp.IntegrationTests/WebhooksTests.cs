using Aeroverra.PayPalSharp.WebhooksV1;
using Xunit;
using Xunit.Abstractions;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Live Webhooks Management tests: the read catalog, listing, a full create -> get ->
/// list-subscribed -> delete -> confirm-gone round-trip, and signature verification.
/// </summary>
[Collection(PayPalCollection.Name)]
public class WebhooksTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public WebhooksTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    [SkippableFact]
    public async Task Event_type_catalog_is_not_empty()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        var catalog = await _fx.Client.Webhooks.WebhooksEventTypesListAsync();

        Assert.NotNull(catalog.EventTypes);
        Assert.NotEmpty(catalog.EventTypes);
        Assert.All(catalog.EventTypes, e => Assert.False(string.IsNullOrWhiteSpace(e.Name)));
        _output.WriteLine($"available event types: {catalog.EventTypes.Count}; e.g. {catalog.EventTypes.First().Name}");
    }

    [SkippableFact]
    public async Task List_webhooks_responds()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        var list = await _fx.Client.Webhooks.ListAsync();

        Assert.NotNull(list); // Webhooks collection may be empty on a fresh app
        _output.WriteLine($"configured webhooks: {list.Webhooks?.Count ?? 0}");
    }

    [SkippableFact]
    public async Task Create_get_subscribed_delete_roundtrip()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        // Unique URL each run so PayPal doesn't reject it as already-registered.
        var url = new Uri($"https://example.com/paypal-webhook/{Guid.NewGuid():N}");
        var request = new Webhook
        {
            Url = url,
            EventTypes = new DefinitionsEventTypeList
            {
                new EventType { Name = "PAYMENT.CAPTURE.COMPLETED" },
                new EventType { Name = "CHECKOUT.ORDER.APPROVED" },
            },
        };

        var created = await _fx.Client.Webhooks.PostAsync(request);
        Assert.False(string.IsNullOrEmpty(created.Id));
        _output.WriteLine($"created webhook id={created.Id}");

        try
        {
            var fetched = await _fx.Client.Webhooks.GetAsync(created.Id);
            Assert.Equal(created.Id, fetched.Id);
            Assert.Equal(url, fetched.Url);

            var subscribed = await _fx.Client.Webhooks.EventTypesListAsync(created.Id);
            Assert.NotEmpty(subscribed.EventTypes);
            Assert.Contains(subscribed.EventTypes, e => e.Name == "PAYMENT.CAPTURE.COMPLETED");
        }
        finally
        {
            await _fx.Client.Webhooks.DeleteAsync(created.Id);
        }

        // Confirm it's gone - a follow-up GET should 404. PayPal returns the typed
        // PayPalApiException<Error2> (a subclass), so accept any PayPalApiException.
        var ex = await Assert.ThrowsAnyAsync<PayPalApiException>(() => _fx.Client.Webhooks.GetAsync(created.Id));
        Assert.Equal(404, ex.StatusCode);
    }

    [SkippableFact]
    public async Task Verify_signature_with_bogus_data_returns_failure()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        var request = new VerifyWebhookSignature
        {
            AuthAlgo = "SHA256withRSA",
            CertUrl = new Uri("https://api.sandbox.paypal.com/v1/notifications/certs/CERT-bogus"),
            TransmissionId = Guid.NewGuid().ToString(),
            TransmissionSig = "not-a-real-signature",
            TransmissionTime = DateTimeOffset.UtcNow,
            WebhookId = _fx.Data("WebhookId") ?? "WH-TEST",
            WebhookEvent = new Event(),
        };

        var response = await _fx.Client.Webhooks.VerifyWebhookSignaturePostAsync(request);

        // The endpoint responds 200 with a FAILURE verdict for a signature that can't verify.
        Assert.Equal("FAILURE", response.VerificationStatus, ignoreCase: true);
        _output.WriteLine($"verification_status={response.VerificationStatus}");
    }
}
