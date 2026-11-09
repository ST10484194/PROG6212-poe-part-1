using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        // Temporary check: 401 means the API answered and is waiting for a login
        var result = await _api.GetAsync<object>("api/events");
        ViewBag.ApiStatus = result.StatusCode;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}