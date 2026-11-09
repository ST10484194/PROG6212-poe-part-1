using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class DashboardController : AppController
{
    private readonly ApiClient _api;

    public DashboardController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
            TempData["Error"] = result.Error ?? "We could not load your events.";

        var events = result.Data ?? new List<EventDto>();
        var now = DateTime.Now;

        var model = new DashboardViewModel
        {
            Upcoming = events.Where(e => e.EventDate >= now).OrderBy(e => e.EventDate).ToList(),
            Past = events.Where(e => e.EventDate < now).OrderByDescending(e => e.EventDate).ToList(),
            TotalEnrolments = events.Sum(e => e.EnrolmentCount)
        };

        return View(model);
    }
}