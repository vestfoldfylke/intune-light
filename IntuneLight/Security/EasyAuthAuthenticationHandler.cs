using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IntuneLight.Security;

// Authentication handler for Azure App Service Easy Auth.
// Supports both interactive users and app-only callers such as Prometheus.
public class EasyAuthAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    // Reuse serializer options to avoid per-request allocations.
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Authenticates the current request using the Easy Auth principal header.
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Logger.LogInformation(
            "EasyAuth headers present: Principal={Principal}, Name={Name}, Id={Id}, Idp={Idp}",
            Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL"),
            Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-NAME"),
            Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-ID"),
            Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-IDP"));

        if (!Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var principalHeader))
        {
            Logger.LogWarning("EasyAuth: X-MS-CLIENT-PRINCIPAL header not present.");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClientPrincipal? payload;

        try
        {
            // Decode the Base64-encoded principal payload from Easy Auth.
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(principalHeader!));

            // Deserialize the Easy Auth principal payload.
            payload = JsonSerializer.Deserialize<ClientPrincipal>(json, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "EasyAuth: Failed to decode or deserialize X-MS-CLIENT-PRINCIPAL.");
            return Task.FromResult(AuthenticateResult.Fail("Invalid Easy Auth principal header."));
        }

        if (payload is null)
        {
            Logger.LogError("EasyAuth: Principal payload was null after deserialization.");
            return Task.FromResult(AuthenticateResult.Fail("Invalid Easy Auth principal payload."));
        }

        // Convert all incoming claims from Easy Auth into Claim objects.
        var claims = payload.Claims?
            .Where(c => !string.IsNullOrWhiteSpace(c.Typ) && !string.IsNullOrWhiteSpace(c.Val))
            .Select(c => new Claim(c.Typ, c.Val))
            .ToList() ?? [];

        // Build identity using the claim types provided by Easy Auth.
        var identity = new ClaimsIdentity(
            claims,
            authenticationType: Scheme.Name,
            nameType: payload.NameTyp ?? ClaimTypes.Name,
            roleType: payload.RoleTyp ?? ClaimTypes.Role);

        var principal = new ClaimsPrincipal(identity);

        var roleClaims = claims
            .Where(c =>
                string.Equals(c.Type, identity.RoleClaimType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "roles", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Extract app-only related claims for Prometheus debugging.
        var hasAppId = claims.Any(c =>
            string.Equals(c.Type, "appid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Type, "azp", StringComparison.OrdinalIgnoreCase));

        var hasObjectId = claims.Any(c =>
            string.Equals(c.Type, "oid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Type, "http://schemas.microsoft.com/identity/claims/objectidentifier", StringComparison.OrdinalIgnoreCase));

        var idType = claims.FirstOrDefault(c =>
            string.Equals(c.Type, "idtyp", StringComparison.OrdinalIgnoreCase))?.Value;

        Logger.LogInformation(
            "EasyAuth auth summary: AuthType={AuthType}, NameType={NameType}, RoleType={RoleType}, ClaimCount={ClaimCount}",
            payload.AuthTyp ?? "(unknown)",
            identity.NameClaimType,
            identity.RoleClaimType,
            claims.Count);

        Logger.LogInformation(
            "EasyAuth app summary: HasAppId={HasAppId}, HasObjectId={HasObjectId}, IdType={IdType}",
            hasAppId,
            hasObjectId,
            idType ?? "(none)");

        Logger.LogInformation(
            "EasyAuth role evaluation: Roles={Roles}, Metrics={Metrics}, User={User}, Admin={Admin}",
            roleClaims.Count > 0 ? string.Join(", ", roleClaims) : "(none)",
            principal.IsInRole("IntuneLight.Metrics"),
            principal.IsInRole("IntuneLight.User"),
            principal.IsInRole("IntuneLight.Admin"));

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Represents the decoded Easy Auth principal payload.
public sealed record ClientPrincipal(
    [property: JsonPropertyName("auth_typ")] string? AuthTyp,
    [property: JsonPropertyName("name_typ")] string? NameTyp,
    [property: JsonPropertyName("role_typ")] string? RoleTyp,
    [property: JsonPropertyName("claims")] List<ClientPrincipalClaim>? Claims);

// Represents a single claim in the Easy Auth principal payload.
public sealed record ClientPrincipalClaim(
    [property: JsonPropertyName("typ")] string Typ,
    [property: JsonPropertyName("val")] string Val);