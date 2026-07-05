using System.Net.Http.Headers;

namespace Aeroverra.PayPalSharp;

/// <summary>
/// Attaches a bearer <c>Authorization</c> header (from <see cref="IPayPalTokenProvider"/>)
/// to every outgoing PayPal request.
/// </summary>
public sealed class PayPalAuthenticationHandler : DelegatingHandler
{
    private readonly IPayPalTokenProvider _tokenProvider;

    public PayPalAuthenticationHandler(IPayPalTokenProvider tokenProvider) => _tokenProvider = tokenProvider;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
