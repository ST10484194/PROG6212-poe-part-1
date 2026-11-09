using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class ManageResultsController : AppController
{
    private readonly ApiClient _api;

    public ManageResultsController(ApiClient api)
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
        var selected = PickEvent(events, eventId, preferPast: true);

        var model = new ManageResultsViewModel
        {
            Selector = new EventSelectorViewModel
            {
                Events = events,
                SelectedEventId = selected?.EventID,
                Controller = "ManageResults"
            },
            Event = selected
        };

        if (selected != null)
        {
            var enrolmentsResult = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{selected.EventID}/enrolments");
            if (SessionExpired(enrolmentsResult) is { } expiredEnrolments) return expiredEnrolments;

            var resultsResult = await _api.GetAsync<List<ResultDto>>($"api/events/{selected.EventID}/results");
            if (SessionExpired(resultsResult) is { } expiredResults) return expiredResults;

            if (!enrolmentsResult.Success || !resultsResult.Success)
                TempData["Error"] = enrolmentsResult.Error ?? resultsResult.Error ?? "We could not load the results.";

            var results = resultsResult.Data ?? new List<ResultDto>();

            model.Rows = (enrolmentsResult.Data ?? new List<EnrolmentDto>())
                .OrderBy(e => e.ParticipantName)
                .Select(e => new ResultRowViewModel
                {
                    Enrolment = e,
                    Result = results.FirstOrDefault(r => r.EnrolmentID == e.EnrolmentID)
                })
                .ToList();
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Capture(CaptureResultViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "Please check the finish time and the finishing position.";
            return RedirectToAction(nameof(Index), new { eventId = model.EventID });
        }

        if (!TimeSpan.TryParse(model.FinishTime, CultureInfo.InvariantCulture, out var finishTime))
        {
            TempData["Error"] = "Enter a valid finish time as hh:mm:ss, for example 01:45:23.";
            return RedirectToAction(nameof(Index), new { eventId = model.EventID });
        }

        var result = await _api.PostAsync<ResultDto>($"api/enrolments/{model.EnrolmentID}/result", new
        {
            finishTime = finishTime.ToString("c", CultureInfo.InvariantCulture),
            finishingPosition = model.FinishingPosition
        });
        if (SessionExpired(result) is { } expired) return expired;

        if (result.Success)
            TempData["Success"] = $"The result for {result.Data?.ParticipantName ?? "the participant"} has been saved and published.";
        else
            TempData["Error"] = result.Error ?? "We could not save the result. Please try again.";

        return RedirectToAction(nameof(Index), new { eventId = model.EventID });
    }
}