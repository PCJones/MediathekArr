using Tvdb.Models;

namespace Tvdb.Clients;

public interface ILoginClient : ITvdbClient
{

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// create an auth token. The token has one month validation length.
    /// </summary>
    /// <returns>response</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    Task<ApiResponseWrapper<Token>> LoginAsync(LoginRequestBody body, CancellationToken cancellationToken = default);

}
