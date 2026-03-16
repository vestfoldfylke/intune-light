namespace IntuneLight.Security;


// Policy name constants used with [Authorize(Policy = Policy.X)] and AddAuthorization.
// These are internal policy names only — the actual role values checked against Entra claims
// are configured via ntraIdOptions (AppRoleAdmin, AppRoleUser, AppRoleMetrics).
public static class Policy
{
    // Policy that requires the Admin role defined in EntraIdOptions.AppRoleAdmin
    public const string Admin = "Admin";

    // Policy that requires the User role defined in EntraIdOptions.AppRoleUser
    public const string User = "User";

    // Policy that requires either the Admin or User role.</summary>
    public const string AnyUserRole = "AnyUserRole";

    // Policy that requires the Metrics role defined in EntraIdOptions.AppRoleMetrics
    public const string Metrics = "Metrics";
}