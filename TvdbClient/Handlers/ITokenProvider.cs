using Newtonsoft.Json.Linq;
using Tvdb.Models;

namespace Tvdb.Handlers;

/// <summary>
/// Provider for Tokens
/// </summary>
public interface ITokenProvider
{
    #region Properties
    Token Token { get; }
    #endregion

    #region Methods
    /// <summary>
    /// Acquire a <see cref="Token"/>
    /// </summary>
    /// <returns></returns>
    public Task<Token> AcquireTokenAsync(CancellationToken cancellationToken = default);
    #endregion
}