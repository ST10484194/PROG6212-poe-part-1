using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole]
public class ProfilePictureController : AppController
{
    private readonly ApiClient _api;

    public ProfilePictureController(ApiClient api)
    {
        _api = api;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? picture)
    {
        var error = ImageValidator.Validate(picture);
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction("Profile", "Account");
        }

        var result = await _api.PostFileAsync<PictureUploadDto>("api/users/me/picture", picture!);
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error ?? "We could not upload your picture. Please try again.";
        }
        else
        {
            HttpContext.Session.SetString(SessionKeys.ProfilePictureUrl, result.Data.ProfilePictureUrl ?? string.Empty);
            TempData["Success"] = "Your profile picture has been updated.";
        }

        return RedirectToAction("Profile", "Account");
    }
}