using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class EventsController : AppController
{
    private static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };

    private readonly ApiClient _api;

    public EventsController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? eventType)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            filters.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (!string.IsNullOrWhiteSpace(eventType) && EventTypes.Contains(eventType))
            filters.Add($"eventType={Uri.EscapeDataString(eventType)}");

        var url = filters.Count > 0 ? "api/events?" + string.Join("&", filters) : "api/events";

        var result = await _api.GetAsync<List<EventDto>>(url);
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
            TempData["Error"] = result.Error ?? "We could not load the events.";

        return View(new EventListViewModel
        {
            Events = result.Data ?? new List<EventDto>(),
            Search = search,
            EventType = EventTypes.Contains(eventType ?? string.Empty) ? eventType : null
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (SessionExpired(eventResult) is { } expired) return expired;

        if (!eventResult.Success || eventResult.Data == null)
        {
            TempData["Error"] = eventResult.NotFound
                ? "That event could not be found."
                : eventResult.Error ?? "We could not load that event.";
            return RedirectToAction(nameof(Index));
        }

        var enrolmentsResult = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
        if (SessionExpired(enrolmentsResult) is { } expiredEnrolments) return expiredEnrolments;

        return View(new EventDetailViewModel
        {
            Event = eventResult.Data,
            MyEnrolment = enrolmentsResult.Data?.FirstOrDefault(e => e.EventID == id)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int id, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category to enter.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _api.PostAsync<EnrolmentDto>($"api/events/{id}/enrolments", new { categoryId });
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "We could not enter you in this event. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = $"You are entered in {result.Data?.EventName ?? "the event"}. Your entry stays pending until the organiser confirms it.";
        return RedirectToAction("Index", "MyEnrolments");
    }
}