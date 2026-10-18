using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Auth;
using RaceDay.Controllers;
using Xunit;

namespace RaceDay.Tests;

public class AuthorizationTests
{
    private static AuthorizationFilterContext CreateContext(int? userId, string? role)
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        if (userId.HasValue) http.Session.SetInt32(SessionKeys.UserId, userId.Value);
        if (role != null) http.Session.SetString(SessionKeys.Role, role);

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void NotLoggedIn_IsRejectedWith401()
    {
        var context = CreateContext(null, null);

        new RequireLoginAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void Participant_IsRejectedFromOrganiserEndpoint_With403()
    {
        var context = CreateContext(2, "Participant");

        new RequireLoginAttribute("Organiser").OnAuthorization(context);

        TestHelper.AssertStatus((IActionResult)context.Result!, 403);
    }

    [Fact]
    public void Organiser_IsRejectedFromParticipantEndpoint_With403()
    {
        var context = CreateContext(1, "Organiser");

        new RequireLoginAttribute("Participant").OnAuthorization(context);

        TestHelper.AssertStatus((IActionResult)context.Result!, 403);
    }

    [Fact]
    public void Organiser_IsAllowedOnOrganiserEndpoint()
    {
        var context = CreateContext(1, "Organiser");

        new RequireLoginAttribute("Organiser").OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void AnyLoggedInUser_IsAllowedWhenNoRoleIsRequired()
    {
        var context = CreateContext(2, "Participant");

        new RequireLoginAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    // Proves the real endpoints are protected with the right role
    [Theory]
    [InlineData(typeof(EventsController), nameof(EventsController.CreateEvent), "Organiser")]
    [InlineData(typeof(EventsController), nameof(EventsController.UpdateEvent), "Organiser")]
    [InlineData(typeof(EventsController), nameof(EventsController.DeleteEvent), "Organiser")]
    [InlineData(typeof(EventsController), nameof(EventsController.GetMyEvents), "Organiser")]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.CreateCategory), "Organiser")]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.UpdateCategory), "Organiser")]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.DeleteCategory), "Organiser")]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetEventEnrolments), "Organiser")]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.UpdateStatus), "Organiser")]
    [InlineData(typeof(ResultsController), nameof(ResultsController.CaptureResult), "Organiser")]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), "Participant")]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetMyEnrolments), "Participant")]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetMyResults), "Participant")]
    public void RoleRestrictedEndpoints_RequireTheCorrectRole(Type controller, string action, string expectedRole)
    {
        var attribute = controller.GetMethod(action)!.GetCustomAttribute<RequireLoginAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedRole, attribute!.Role);
    }
}