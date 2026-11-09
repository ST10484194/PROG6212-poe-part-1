using Microsoft.AspNetCore.Mvc;
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
}
namespace RaceDay.Web.Controllers
{
    public class AccountController
    {
    }
}
