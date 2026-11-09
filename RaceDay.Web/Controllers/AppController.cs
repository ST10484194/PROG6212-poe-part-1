using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class AppController : Controller
{
    protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId);
    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

    /// <summary>
    /// If the API says the login has gone (it restarted or timed out), log the user out here too
    /// and send them to the login page. Returns null when everything is fine.
    /// </summary>
    protected IActionResult? SessionExpired<T>(ApiResult<T> result)
    {
        if (!result.Unauthorized) return null;

        HttpContext.Session.Clear();
        TempData["Error"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Account");
    }
}