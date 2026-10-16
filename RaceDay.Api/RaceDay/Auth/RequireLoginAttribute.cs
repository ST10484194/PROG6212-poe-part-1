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
    private readonly string? _role;

    public RequireLoginAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in." });
            return;
        }

        if (_role != null && session.GetString(SessionKeys.Role) != _role)
        {
            context.Result = new ObjectResult(new { message = $"This action is for {_role}s only." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}