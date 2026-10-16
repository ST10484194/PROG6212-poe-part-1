using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Auth;

public static class SessionKeys
{
    public const string UserId = "UserID";
    public const string Role = "Role";
}

/// <summary>
/// [RequireLogin] = any logged-in user. [RequireLogin("Organiser")] = that role only.
/// Returns 401 if no session, 403 if the role does not match.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireLoginAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; }

    public RequireLoginAttribute(string? role = null)
    {
        Role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in." });
            return;
        }

        if (Role != null && session.GetString(SessionKeys.Role) != Role)
        {
            context.Result = new ObjectResult(new { message = $"This action is for {Role}s only." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}