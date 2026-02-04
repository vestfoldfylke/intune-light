using IntuneLight.Models.Options;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace IntuneLight.Services;

public interface ITokenService
{
    Task<string> GetGraphTokenAsync();
    Task<string> GetDefenderTokenAsync();
    Task<string> GetPureserviceTokenAsync();
}

public sealed class TokenService : ITokenService
{
    // Dependencies
    private readonly EntraIdOptions _options;
    private readonly IConfidentialClientApplication _app;
    private readonly ILogger<TokenService> _logger;

    // Constructor
    public TokenService(IOptions<EntraIdOptions> options, ILogger<TokenService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _app = ConfidentialClientApplicationBuilder
            .Create(_options.ClientId)
            .WithClientSecret(_options.ClientSecret)
            .WithAuthority(_options.Authority)
            .Build();
    }

    // Fetch token for Microsoft Graph
    public async Task<string> GetGraphTokenAsync()
    {
        try
        {
            var scopes = new[] { _options.GraphScope };
            var result = await _app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }
        catch (MsalServiceException ex)
        {

            _logger.LogError(ex,
                    "Graph token request failed: ErrorCode={ErrorCode}, StatusCode={StatusCode}, Response={Response}",
                    ex.ErrorCode,
                    ex.StatusCode,
                    ex.ResponseBody);
            
            throw;
        }
    }

    // Fetch token for Microsoft Defender
    public async Task<string> GetDefenderTokenAsync()
    {
        try
        {
            var scopes = new[] { _options.DefenderScope };
            var result = await _app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }
        catch (MsalServiceException ex)
        {

            _logger.LogError(ex,
                    "Defender token request failed: ErrorCode={ErrorCode}, StatusCode={StatusCode}, Response={Response}",
                    ex.ErrorCode,
                    ex.StatusCode,
                    ex.ResponseBody);

            throw;
        }
    }

    // Fetch token for Pureservice from environment variable
    public Task<string> GetPureserviceTokenAsync()
    {
        try
        {
            var token = Environment.GetEnvironmentVariable("PURESERVICE_API_KEY")?
                                   .Replace("\r", "")
                                   .Replace("\n", "")
                                   .Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("Missing environment variable: PURESERVICE_API_KEY");
                throw new InvalidOperationException("Pureservice API token er ikke konfigurert.");
            }

            return Task.FromResult(token);
        }
        catch (Exception)
        {
            throw;
        }
    }
}