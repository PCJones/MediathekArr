using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tvdb.Configuration;
using Tvdb.Handlers;
using Tvdb.Models;

namespace Tvdb.Provider;

public class TvdbTokenProvider(IOptions<TvdbConfiguration> options, ILogger<TvdbTokenProvider> logger) : ITokenProvider
{
    #region Properties
    public TvdbConfiguration Config => Options.Value;
    public IOptions<TvdbConfiguration> Options { get; } = options;
    public ILogger<TvdbTokenProvider> Logger { get; } = logger;
    public Token Token { get; internal set; }
    #endregion

    #region Methods
    /// <inheritdoc/>
    public async Task<Token> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        /* Acquire new Token */
        if (Token is null || Token.IsTokenExpired)
        {
            try
            {
                var httpClient = new HttpClient();
                var requestBody = new StringContent(JsonSerializer.Serialize(new { apikey = Config.ApiKey }), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

                var response = await httpClient.PostAsync(Config.TokenUrl, requestBody, cancellationToken);
                if (!response.IsSuccessStatusCode) Logger.LogError("Failed acquiring Token");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<ApiResponseWrapper<Token>>(responseBody);
                if(!responseData.IsSuccess)
                {
                    Logger.LogError($"Failed acquiring Token. {responseData.ErrorMessage}");
                    throw new Exception($"Failed acquiring Token. {responseData.ErrorMessage}");
                }
                var token = responseData.Data;
                if (token is null)
                {
                    Logger.LogError("Failed deserializing Token response");
                    throw new Exception("Failed deserializing Token response");
                }
                Token = token;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed acquiring token. {errorMessage}", ex.Message);
                throw;
            }
        }

        return Token;
    }
    #endregion
}