using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tvdb.Handlers;
public class TokenAuthorizationHeaderHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    #region Properties
    public ITokenProvider TokenProvider { get; } = tokenProvider;
    #endregion

    #region Overrides
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await TokenProvider.AcquireTokenAsync(cancellationToken);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(token.TokenType, token.AccessToken);
        return await base.SendAsync(request, cancellationToken);
    }
    #endregion
}