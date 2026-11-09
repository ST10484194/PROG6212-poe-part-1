using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class ManageCategoriesController : AppController
{
    private readonly ApiClient _api;

    public ManageCategoriesController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventId)
    {
        var (model, redirect) = await BuildModel(eventId, null);
        return redirect ?? View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var (invalidModel, invalidRedirect) = await BuildModel(form.EventID, form);
            return invalidRedirect ?? View("Index", invalidModel);
        }

        var result = await _api.PostAsync<CategoryDto>($"api/events/{form.EventID}/categories", ToRequest(form));
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
        {
            if (result.Forbidden || result.NotFound)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "We could not add the category. Please try again.");
            var (failedModel, failedRedirect) = await BuildModel(form.EventID, form);
            return failedRedirect ?? View("Index", failedModel);
        }

        TempData["Success"] = $"The category \"{form.CategoryName}\" has been added.";
        return RedirectToAction(nameof(Index), new { eventId = form.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var result = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
        if (SessionExpired(result) is { } expired) return expired;

        var category = result.Data?.FirstOrDefault(c => c.CategoryID == id);
        if (!result.Success || category == null)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        return View(new CategoryFormViewModel
        {
            CategoryID = category.CategoryID,
            EventID = category.EventID,
            CategoryName = category.CategoryName,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel form)
    {
        form.CategoryID = id;
        if (!ModelState.IsValid) return View(form);

        var result = await _api.PutAsync<CategoryDto>($"api/categories/{id}", ToRequest(form));
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success)
        {
            if (result.Forbidden || result.NotFound)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index), new { eventId = form.EventID });
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "We could not update the category. Please try again.");
            return View(form);
        }

        TempData["Success"] = $"The category \"{form.CategoryName}\" has been updated.";
        return RedirectToAction(nameof(Index), new { eventId = form.EventID });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await _api.DeleteAsync($"api/categories/{id}");
        if (SessionExpired(result) is { } expired) return expired;

        if (result.Success)
            TempData["Success"] = "The category has been deleted.";
        else
            TempData["Error"] = result.Error ?? "We could not delete the category. Please try again.";

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<(ManageCategoriesViewModel? Model, IActionResult? Redirect)> BuildModel(int? eventId, CategoryFormViewModel? form)
    {
        var eventsResult = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (SessionExpired(eventsResult) is { } expired) return (null, expired);

        if (!eventsResult.Success)
            TempData["Error"] = eventsResult.Error ?? "We could not load your events.";

        var events = eventsResult.Data ?? new List<EventDto>();
        var selected = PickEvent(events, eventId);

        var model = new ManageCategoriesViewModel
        {
            Selector = new EventSelectorViewModel
            {
                Events = events,
                SelectedEventId = selected?.EventID,
                Controller = "ManageCategories"
            },
            Event = selected,
            NewCategory = form ?? new CategoryFormViewModel()
        };

        if (selected != null)
        {
            model.NewCategory.EventID = selected.EventID;

            var categoriesResult = await _api.GetAsync<List<CategoryDto>>($"api/events/{selected.EventID}/categories");
            if (SessionExpired(categoriesResult) is { } expiredCategories) return (null, expiredCategories);

            if (!categoriesResult.Success)
                TempData["Error"] = categoriesResult.Error ?? "We could not load the categories.";

            model.Categories = categoriesResult.Data ?? new List<CategoryDto>();
        }

        return (model, null);
    }

    private static object ToRequest(CategoryFormViewModel f) => new
    {
        categoryName = f.CategoryName,
        minAge = f.MinAge,
        maxAge = f.MaxAge,
        distanceKm = f.DistanceKm
    };
}