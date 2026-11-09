using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentRole != null) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<UserDto>("api/auth/register", new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            contactNumber = string.IsNullOrWhiteSpace(model.ContactNumber) ? null : model.ContactNumber
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not create your account. Please try again.");
            return View(model);
        }

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentRole != null) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Start from a clean session so no old login can leak into this one
        HttpContext.Session.Clear();

        var result = await _api.PostAsync<UserDto>("api/auth/login", new
        {
            email = model.Email,
            password = model.Password
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not log you in. Please try again.");
            return View(model);
        }

        var user = result.Data;
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);
        HttpContext.Session.SetString(SessionKeys.ProfilePictureUrl, user.ProfilePictureUrl ?? string.Empty);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (CurrentRole != null)
            await _api.PostAsync<object>("api/auth/logout");

        _api.ForgetSession();
        HttpContext.Session.Clear();

        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet, RequireRole]
    public async Task<IActionResult> Profile()
    {
        var result = await _api.GetAsync<UserDto>("api/users/me");
        if (SessionExpired(result) is { } expired) return expired;

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error ?? "We could not load your profile.";
            return RedirectToAction("Index", "Home");
        }

        return View(ToProfile(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken, RequireRole]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        // Email, role and picture are read from the API, never trusted from the form
        var current = await _api.GetAsync<UserDto>("api/users/me");
        if (SessionExpired(current) is { } expired) return expired;

        if (!current.Success || current.Data == null)
        {
            TempData["Error"] = current.Error ?? "We could not load your profile.";
            return RedirectToAction("Index", "Home");
        }

        model.Email = current.Data.Email;
        model.Role = current.Data.Role;
        model.ProfilePictureUrl = current.Data.ProfilePictureUrl;

        if (!ModelState.IsValid) return View(model);

        var result = await _api.PutAsync<UserDto>("api/users/me", new
        {
            fullName = model.FullName,
            contactNumber = string.IsNullOrWhiteSpace(model.ContactNumber) ? null : model.ContactNumber,
            profilePictureUrl = model.ProfilePictureUrl
        });
        if (SessionExpired(result) is { } expiredUpdate) return expiredUpdate;

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not update your profile. Please try again.");
            return View(model);
        }

        HttpContext.Session.SetString(SessionKeys.FullName, result.Data.FullName);
        HttpContext.Session.SetString(SessionKeys.ProfilePictureUrl, result.Data.ProfilePictureUrl ?? string.Empty);

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    private static ProfileViewModel ToProfile(UserDto u) => new()
    {
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        ContactNumber = u.ContactNumber,
        ProfilePictureUrl = u.ProfilePictureUrl
    };
}