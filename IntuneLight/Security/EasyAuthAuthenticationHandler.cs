using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IntuneLight.Security;

// Authentication handler for Azure App Service Easy Auth.
public class EasyAuthAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    // The main authentication logic is implemented in HandleAuthenticateAsync, which is called by the authentication middleware.
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Log the presence of EasyAuth headers for debugging purposes.
        Logger.LogInformation("EasyAuth headers present: Principal={Principal}, Name={Name}, Id={Id}",
            Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL"),
            Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-NAME"),
            Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-ID"));

        // Check if the "X-MS-CLIENT-PRINCIPAL" header is present. If not, return no result to indicate that this handler did not authenticate the request.
        if (!Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var principal))
        {
            Logger.LogWarning("EasyAuth: X-MS-CLIENT-PRINCIPAL header not present.");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Decode the base64-encoded JSON string from the header and deserialize it into a ClientPrincipal object.
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(principal!));

        // Deserialize the JSON into a ClientPrincipal object. If deserialization fails, log an error and return no result.
        var payload = JsonSerializer.Deserialize<ClientPrincipal>(json);
        if (payload == null)
        {
            Logger.LogError("EasyAuth: Failed to deserialize principal JSON.");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Define the claim types we care about.
        var relevantTypes = new HashSet<string>
        {
            "http://schemas.microsoft.com/identity/claims/objectidentifier",
            "preferred_username",
            "name",
            "roles"
        };

        // Extract the relevant claims from the payload and create a list of Claim objects.
        var claims = payload.Claims
            .Where(c => relevantTypes.Contains(c.Typ))
            .Select(c => new Claim(c.Typ, c.Val))
            .ToList() ?? [];

        // Easy Auth provides the claims, so we can trust them and set the authentication type to "EasyAuth".
        var identity = new ClaimsIdentity(claims, "EasyAuth",
            nameType: "preferred_username",
            roleType: "roles");

        // Log the role checks for Metrics, User, and Admin roles to verify that the ClaimsPrincipal is correctly interpreting the roles.
        Logger.LogInformation("EasyAuth: IsInRole Metrics={Metrics}, User={User}, Admin={Admin}",
        new ClaimsPrincipal(identity).IsInRole("Metrics"),
        new ClaimsPrincipal(identity).IsInRole("User"),
        new ClaimsPrincipal(identity).IsInRole("Admin"));

        // Create an authentication ticket with the ClaimsPrincipal and return success.
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "EasyAuth");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

record ClientPrincipal(
    [property: JsonPropertyName("claims")] List<ClientPrincipalClaim> Claims);

record ClientPrincipalClaim(
    [property: JsonPropertyName("typ")] string Typ,
    [property: JsonPropertyName("val")] string Val);