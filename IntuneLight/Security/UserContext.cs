using System.Security.Claims;

namespace IntuneLight.Security;

// Provides easy access to user information from the current HTTP context, especially when running behind Azure App Service Easy Auth.
public sealed class UserContext(IHttpContextAccessor http)
{
    // The User property retrieves the ClaimsPrincipal from the current HTTP context, if available.
    private ClaimsPrincipal? User => http.HttpContext?.User;

    // ObjectId is a unique identifier for the user provided by Azure AD.
    // It tries to find the claim using both the standard URI and the "oid" shorthand.
    public string? ObjectId => User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                               ?? User?.FindFirstValue("oid");
    
    // Upn is the user's principal name, typically their email address.
    // It tries to find the claim using both the standard URI and the ClaimTypes.Email shorthand.
    public string? Upn => User?.FindFirstValue("preferred_username")
                          ?? User?.FindFirstValue(ClaimTypes.Email);

    public string DisplayName => User?.FindFirstValue("name") ?? Upn ?? "Ukjent bruker";

    // Actor is used for logging and metrics. It prioritizes UPN, then ObjectId, and falls back to "anonymous" if neither is available.
    public string Actor => Upn ?? ObjectId ?? "anonymous";
}