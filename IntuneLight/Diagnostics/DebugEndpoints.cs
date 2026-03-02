using System.Security.Claims;

namespace IntuneLight.Diagnostics;

// Provides debug endpoints for inspecting the current user's identity and claims.
internal static class DebugEndpoints
{
    public static IResult WhoAmI(HttpContext ctx)
    {
        var user = ctx.User;

        // Return a JSON object containing information about the current user's authentication status, name, relevant claims, and their values.
        return Results.Json(new
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            user.Identity?.Name,
            Actor = new
            {
                Oid = user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier"),
                Upn = user.FindFirstValue("preferred_username") ?? user.FindFirstValue(ClaimTypes.Email)
            },
            Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }
}