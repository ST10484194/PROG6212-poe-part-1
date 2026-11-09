using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class MyResultsController : AppController
{
    private readonly ApiClient _api;

    public MyResultsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<ResultDto>>("api/users/me/results");
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
            TempData["Error"] = result.Error ?? "We could not load your results.";

        return View(new MyResultsViewModel
        {
            Results = (result.Data ?? new List<ResultDto>()).OrderByDescending(r => r.EventDate).ToList()
        });
    }
}