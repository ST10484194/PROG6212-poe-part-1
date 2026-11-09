using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class MyEnrolmentsController : AppController
{
    private readonly ApiClient _api;

    public MyEnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
            TempData["Error"] = result.Error ?? "We could not load your enrolments.";

        var enrolments = result.Data ?? new List<EnrolmentDto>();
        var now = DateTime.Now;

        return View(new MyEnrolmentsViewModel
        {
            Upcoming = enrolments.Where(e => e.EventDate >= now).OrderBy(e => e.EventDate).ToList(),
            Past = enrolments.Where(e => e.EventDate < now).OrderByDescending(e => e.EventDate).ToList()
        });
    }
}