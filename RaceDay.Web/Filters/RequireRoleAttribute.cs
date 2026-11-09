using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// [RequireRole] = any logged-in user. [RequireRole("Organiser")] = that role only.
/// Visitors who are not logged in go to the login page, and the wrong role goes to an access denied page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public RequireRoleAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var role = http.Session.GetString(SessionKeys.Role);

        if (string.IsNullOrEmpty(role))
        {
            // Remember where they were going, but only for plain page views
            var returnUrl = http.Request.Method == HttpMethods.Get
                ? http.Request.Path + http.Request.QueryString
                : null;

            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_role != null && role != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}