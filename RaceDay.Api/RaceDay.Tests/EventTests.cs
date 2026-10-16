using Microsoft.AspNetCore.Mvc;
using RaceDay.Controllers;
using RaceDay.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class EventTests
{
    private static EventRequest ValidRequest(string name = "Heritage Run") => new()
    {
        Name = name,
        Description = "A road run through Thohoyandou.",
        EventDate = DateTime.Now.AddDays(30),
        Location = "Thohoyandou, Limpopo",
        DistanceKm = 21.1m,
        EventType = "Run"
    };

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201AndSavesEvent()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "o@test.com", "Organiser");
        var controller = TestHelper.WithSession(new EventsController(db), organiser.UserID, "Organiser");

        var result = await controller.CreateEvent(ValidRequest());

        TestHelper.AssertStatus(result, 201);
        var saved = db.Events.Single();
        Assert.Equal("Heritage Run", saved.Name);
        Assert.Equal(organiser.UserID, saved.OrganiserID);
    }

    [Fact]
    public async Task UpdateEvent_ByOwner_Returns200AndChangesTheEvent()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "o@test.com", "Organiser");
        var ev = TestHelper.AddEvent(db, organiser.UserID);
        var controller = TestHelper.WithSession(new EventsController(db), organiser.UserID, "Organiser");

        var result = await controller.UpdateEvent(ev.EventID, ValidRequest("Renamed Run"));

        TestHelper.AssertStatus(result, 200);
        Assert.Equal("Renamed Run", db.Events.Single().Name);
    }

    [Fact]
    public async Task UpdateEvent_ByDifferentOrganiser_Returns403AndChangesNothing()
    {
        using var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, "owner@test.com", "Organiser");
        var other = TestHelper.AddUser(db, "other@test.com", "Organiser");
        var ev = TestHelper.AddEvent(db, owner.UserID);
        var controller = TestHelper.WithSession(new EventsController(db), other.UserID, "Organiser");

        var result = await controller.UpdateEvent(ev.EventID, ValidRequest("Hijacked"));

        TestHelper.AssertStatus(result, 403);
        Assert.Equal("Test Run", db.Events.Single().Name);
    }

    [Fact]
    public async Task DeleteEvent_ByOwner_Returns204AndRemovesEvent()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "o@test.com", "Organiser");
        var ev = TestHelper.AddEvent(db, organiser.UserID);
        var controller = TestHelper.WithSession(new EventsController(db), organiser.UserID, "Organiser");

        var result = await controller.DeleteEvent(ev.EventID);

        TestHelper.AssertStatus(result, 204);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task DeleteEvent_ByDifferentOrganiser_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, "owner@test.com", "Organiser");
        var other = TestHelper.AddUser(db, "other@test.com", "Organiser");
        var ev = TestHelper.AddEvent(db, owner.UserID);
        var controller = TestHelper.WithSession(new EventsController(db), other.UserID, "Organiser");

        var result = await controller.DeleteEvent(ev.EventID);

        TestHelper.AssertStatus(result, 403);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "o@test.com", "Organiser");
        var participant = TestHelper.AddUser(db, "p@test.com", "Participant");
        var ev = TestHelper.AddEvent(db, organiser.UserID);
        var category = TestHelper.AddCategory(db, ev.EventID);
        TestHelper.AddEnrolment(db, participant.UserID, ev.EventID, category.CategoryID);
        var controller = TestHelper.WithSession(new EventsController(db), organiser.UserID, "Organiser");

        var result = await controller.DeleteEvent(ev.EventID);

        TestHelper.AssertStatus(result, 409);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.WithSession(new EventsController(db), userId: 1, role: "Participant");

        var result = await controller.GetEvent(9999);

        TestHelper.AssertStatus(result, 404);
    }

    [Fact]
    public async Task GetUpcomingEvents_ReturnsOnlyFutureEvents()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "o@test.com", "Organiser");
        TestHelper.AddEvent(db, organiser.UserID, DateTime.Now.AddDays(10));
        TestHelper.AddEvent(db, organiser.UserID, DateTime.Now.AddDays(-10));
        var controller = TestHelper.WithSession(new EventsController(db), organiser.UserID, "Organiser");

        var result = await controller.GetUpcomingEvents(null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<List<EventResponse>>(ok.Value);
        Assert.Single(events);
    }
}