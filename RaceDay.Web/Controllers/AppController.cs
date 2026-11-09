using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
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

    /// <summary>
    /// Returns the requested event if it is in the list, otherwise a sensible default:
    /// the next upcoming event (or, with preferPast, the most recent past one).
    /// </summary>
    protected static EventDto? PickEvent(List<EventDto> events, int? eventId, bool preferPast = false)
    {
        if (eventId.HasValue)
        {
            var requested = events.FirstOrDefault(e => e.EventID == eventId.Value);
            if (requested != null) return requested;
        }

        var now = DateTime.Now;
        var upcoming = events.Where(e => e.EventDate >= now).OrderBy(e => e.EventDate).FirstOrDefault();
        var past = events.Where(e => e.EventDate < now).OrderByDescending(e => e.EventDate).FirstOrDefault();

        return preferPast ? (past ?? upcoming) : (upcoming ?? past);
    }
}