using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class ManageEnrolmentsController : AppController
{
    private readonly ApiClient _api;

    public ManageEnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventId)
    {
        var eventsResult = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (SessionExpired(eventsResult) is { } expired) return expired;

        if (!eventsResult.Success)
            TempData["Error"] = eventsResult.Error ?? "We could not load your events.";

        var events = eventsResult.Data ?? new List<EventDto>();
        var selected = PickEvent(events, eventId);

        var model = new ManageEnrolmentsViewModel
        {
            Selector = new EventSelectorViewModel
            {
                Events = events,
                SelectedEventId = selected?.EventID,
                Controller = "ManageEnrolments"
            },
            Event = selected
        };

        if (selected != null)
        {
            var enrolmentsResult = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{selected.EventID}/enrolments");
            if (SessionExpired(enrolmentsResult) is { } expiredEnrolments) return expiredEnrolments;

            if (!enrolmentsResult.Success)
                TempData["Error"] = enrolmentsResult.Error ?? "We could not load the enrolments.";

            model.Enrolments = (enrolmentsResult.Data ?? new List<EnrolmentDto>())
                .OrderBy(e => e.EnrolmentDate)
                .ToList();
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, int eventId, string status)
    {
        var result = await _api.PutAsync<EnrolmentDto>($"api/enrolments/{id}/status", new { status });
        if (SessionExpired(result) is { } expired) return expired;

        if (result.Success)
        {
            var name = result.Data?.ParticipantName ?? "The participant";
            TempData["Success"] = status == "Confirmed"
                ? $"{name}'s enrolment has been confirmed."
                : $"{name}'s enrolment has been set back to Pending.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "We could not update the enrolment. Please try again.";
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}