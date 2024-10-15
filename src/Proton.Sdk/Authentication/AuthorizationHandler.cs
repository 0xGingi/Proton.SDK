using System.Net.Http.Headers;

namespace Proton.Sdk.Authentication;

internal sealed class AuthorizationHandler(ProtonApiSession session) : DelegatingHandler
{
    private const string SessionIdHeaderName = "x-pm-uid";

    private readonly ProtonApiSession _session = session;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _session.TokenCredential.GetTokenAsync(cancellationToken).ConfigureAwait(false);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add(SessionIdHeaderName, _session.Id);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
