using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class ManageEventsController : AppController
{
    private readonly ApiClient _api;

    public ManageEventsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
            TempData["Error"] = result.Error ?? "We could not load your events.";

        return View(result.Data ?? new List<EventDto>());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new EventFormViewModel { EventDate = DateTime.Today.AddMonths(1).AddHours(7) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var result = await _api.PostAsync<EventDto>("api/events", ToRequest(model));
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not create the event. Please try again.");
            return View("Form", model);
        }

        TempData["Success"] = $"\"{model.Name}\" has been created. You can now add categories to it.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.NotFound ? "That event could not be found." : result.Error;
            return RedirectToAction(nameof(Index));
        }

        var ev = result.Data;
        if (ev.OrganiserID != CurrentUserId)
        {
            TempData["Error"] = "You can only edit your own events.";
            return RedirectToAction(nameof(Index));
        }

        return View("Form", new EventFormViewModel
        {
            EventID = ev.EventID,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            BannerImageUrl = ev.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventID = id;
        if (!ModelState.IsValid) return View("Form", model);

        var result = await _api.PutAsync<EventDto>($"api/events/{id}", ToRequest(model));
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
        {
            if (result.Forbidden || result.NotFound)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "We could not update the event. Please try again.");
            return View("Form", model);
        }

        TempData["Success"] = $"\"{model.Name}\" has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteAsync($"api/events/{id}");
        if (SessionExpired(result) is { } expired) return expired;

        if (result.Success)
            TempData["Success"] = "The event has been deleted.";
        else
            TempData["Error"] = result.Error ?? "We could not delete the event. Please try again.";

        return RedirectToAction(nameof(Index));
    }

    private static object ToRequest(EventFormViewModel m) => new
    {
        name = m.Name,
        description = m.Description,
        eventDate = m.EventDate,
        location = m.Location,
        distanceKm = m.DistanceKm,
        eventType = m.EventType
    };
}