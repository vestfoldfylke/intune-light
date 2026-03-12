using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IntuneLight.Security;

// Authentication handler used in Development only.
// Auto-authenticates all requests with a mock user so the app can be tested locally
// without Easy Auth or Entra ID. Replaced by <see cref="EasyAuthAuthenticationHandler"/> in production.
public class MockAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new("preferred_username", "dev.user@vestfoldfylke.no"),
            new("name", "Dev User"),
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", "dev-oid-1234"),
            new("roles", "User")
        };
        var identity = new ClaimsIdentity(claims, "Mock",
            nameType: "preferred_username",
            roleType: "roles");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Mock");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
