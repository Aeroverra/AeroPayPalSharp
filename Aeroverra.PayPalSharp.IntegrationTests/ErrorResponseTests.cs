using System.Net;
using Aeroverra.PayPalSharp;
using Aeroverra.PayPalSharp.OrdersV2;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// A failed call surfaces PayPal's error body: the raw JSON is on PayPalApiException.Response (so callers
/// can read details[].issue), and the typed Result carries the same. No network.
/// </summary>
public class ErrorResponseTests
{
    private const string ErrorBody =
        "{\"name\":\"UNPROCESSABLE_ENTITY\",\"details\":[{\"issue\":\"ORDER_NOT_APPROVED\"," +
        "\"description\":\"Payer has not yet approved the Order for payment.\"}]," +
        "\"message\":\"The requested action could not be performed.\",\"debug_id\":\"abc123\"}";

    private sealed class Error422Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage((HttpStatusCode)422)
            {
                Content = new StringContent(ErrorBody, System.Text.Encoding.UTF8, "application/json"),
            });

    }

    [Fact]
    public async Task Error_body_is_available_on_the_exception()
    {
        var http = new HttpClient(new Error422Handler()) { BaseAddress = new Uri("https://api-m.sandbox.paypal.com") };
        var client = new OrdersV2Client(http);

        var ex = await Assert.ThrowsAnyAsync<PayPalApiException>(() => client.CaptureAsync("ORDER-ID"));

        Assert.Equal(422, ex.StatusCode);
        // The raw JSON body is now captured (was empty before), so a caller can parse the real issue.
        Assert.Contains("ORDER_NOT_APPROVED", ex.Response);

        // The typed error carries it too.
        var typed = ex as PayPalApiException<Error>;
        Assert.NotNull(typed);
        Assert.Equal("ORDER_NOT_APPROVED", typed!.Result.Details!.First().Issue);
    }
}
