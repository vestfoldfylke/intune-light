using System.Security.Claims;
using System.Text.Encodings.Web;
using IntuneLight.Models.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IntuneLight.Security;

// Authentication handler used in Development only.
// Auto-authenticates all requests with a mock user so the app can be tested locally
// without Easy Auth or Entra ID. Replaced by <see cref="EasyAuthAuthenticationHandler"/> in production.
public class MockAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get mock authentication options from configuration, or use defaults if not specified
        var mockOptions = configuration.GetSection("MockAuth").Get<MockAuthOptions>() ?? new MockAuthOptions();

        // Create claims based on the mock user information from configuration
        var claims = new List<Claim>
        {
            new("preferred_username", mockOptions.Username),
            new("name", mockOptions.Name),
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", mockOptions.ObjectId)
        };

        // Add roles as claims if specified in configuration
        claims.AddRange(mockOptions.Roles.Select(r => new Claim("roles", r)));

        // Create a ClaimsIdentity with the specified claims and authentication type "Mock"
        var identity = new ClaimsIdentity(claims, "Mock",
            nameType: "preferred_username",
            roleType: "roles");

        // Create an AuthenticationTicket with the ClaimsPrincipal and authentication scheme "Mock"
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Mock");

        // Return a successful authentication result with the created ticket
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}