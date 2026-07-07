using Microsoft.Extensions.Options;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Attaches the <c>PayPal-Mock-Response</c> header (from an active <c>WithMockResponse</c> scope) so the
/// sandbox returns a forced negative/error response for that call. SAFETY: it only ever sends the header
/// when the client is pointed at the SANDBOX, so a mock scope accidentally left in code can never change a
/// live call. A caller-set header is left untouched.
/// </summary>
public sealed class PayPalMockResponseHandler : DelegatingHandler
{
    private const string Header = "PayPal-Mock-Response";

    private readonly PayPalOptions _options;
    private readonly PayPalMockResponseContext _context;

    public PayPalMockResponseHandler(IOptions<PayPalOptions> options, PayPalMockResponseContext context)
    {
        _options = options.Value;
        _context = context;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var value = _context.CurrentHeaderValue;
        if (_options.Environment == PayPalEnvironment.Sandbox
            && !string.IsNullOrWhiteSpace(value)
            && !request.Headers.Contains(Header))
        {
            request.Headers.TryAddWithoutValidation(Header, value);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
