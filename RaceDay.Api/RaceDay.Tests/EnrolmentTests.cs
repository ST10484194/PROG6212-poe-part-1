using Microsoft.AspNetCore.Mvc;
using RaceDay.Controllers;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentTests
{
    private record SeedData(User Organiser, User Participant, Event Ev, Category Cat);

    private static SeedData Seed(AppDbContext db, DateTime? eventDate = null)
    {
        var organiser = TestHelper.AddUser(db, "organiser@test.com", "Organiser");
        var participant = TestHelper.AddUser(db, "participant@test.com", "Participant");
        var ev = TestHelper.AddEvent(db, organiser.UserID, eventDate);
        var cat = TestHelper.AddCategory(db, ev.EventID);
        return new SeedData(organiser, participant, ev, cat);
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201AndRecordsPendingEnrolment()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), s.Participant.UserID, "Participant");

        var result = await controller.Enrol(s.Ev.EventID, new EnrolmentRequest { CategoryId = s.Cat.CategoryID });

        TestHelper.AssertStatus(result, 201);
        var enrolment = db.Enrolments.Single();
        Assert.Equal(s.Participant.UserID, enrolment.ParticipantID);
        Assert.Equal(s.Ev.EventID, enrolment.EventID);
        Assert.Equal(s.Cat.CategoryID, enrolment.CategoryID);
        Assert.Equal("Pending", enrolment.EnrolmentStatus);
    }

    [Fact]
    public async Task Enrol_TwiceInSameEvent_Returns409AndKeepsOneEnrolment()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), s.Participant.UserID, "Participant");
        var request = new EnrolmentRequest { CategoryId = s.Cat.CategoryID };
        await controller.Enrol(s.Ev.EventID, request);

        var result = await controller.Enrol(s.Ev.EventID, request);

        TestHelper.AssertStatus(result, 409);
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_CategoryBelongingToAnotherEvent_Returns404()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db);
        var otherEvent = TestHelper.AddEvent(db, s.Organiser.UserID);
        var otherCategory = TestHelper.AddCategory(db, otherEvent.EventID, "Other");
        var controller = TestHelper.WithSession(new EnrolmentsController(db), s.Participant.UserID, "Participant");

        var result = await controller.Enrol(s.Ev.EventID, new EnrolmentRequest { CategoryId = otherCategory.CategoryID });

        TestHelper.AssertStatus(result, 404);
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_UnknownEvent_Returns404()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), s.Participant.UserID, "Participant");

        var result = await controller.Enrol(9999, new EnrolmentRequest { CategoryId = s.Cat.CategoryID });

        TestHelper.AssertStatus(result, 404);
    }

    [Fact]
    public async Task Enrol_PastEvent_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db, DateTime.Now.AddDays(-5));
        var controller = TestHelper.WithSession(new EnrolmentsController(db), s.Participant.UserID, "Participant");

        var result = await controller.Enrol(s.Ev.EventID, new EnrolmentRequest { CategoryId = s.Cat.CategoryID });

        TestHelper.AssertStatus(result, 400);
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task GetMyEnrolments_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db);
        var second = TestHelper.AddUser(db, "second@test.com", "Participant");
        TestHelper.AddEnrolment(db, s.Participant.UserID, s.Ev.EventID, s.Cat.CategoryID);
        TestHelper.AddEnrolment(db, second.UserID, s.Ev.EventID, s.Cat.CategoryID);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), s.Participant.UserID, "Participant");

        var result = await controller.GetMyEnrolments();

        var ok = Assert.IsType<OkObjectResult>(result);
        var enrolments = Assert.IsAssignableFrom<List<EnrolmentResponse>>(ok.Value);
        Assert.Single(enrolments);
        Assert.Equal(s.Participant.UserID, enrolments[0].ParticipantID);
    }

    [Fact]
    public async Task GetEventEnrolments_ByOwner_ReturnsTheEnrolments()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db);
        TestHelper.AddEnrolment(db, s.Participant.UserID, s.Ev.EventID, s.Cat.CategoryID);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), s.Organiser.UserID, "Organiser");

        var result = await controller.GetEventEnrolments(s.Ev.EventID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var enrolments = Assert.IsAssignableFrom<List<EnrolmentResponse>>(ok.Value);
        Assert.Single(enrolments);
    }

    [Fact]
    public async Task GetEventEnrolments_ByDifferentOrganiser_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db);
        var other = TestHelper.AddUser(db, "other@test.com", "Organiser");
        var controller = TestHelper.WithSession(new EnrolmentsController(db), other.UserID, "Organiser");

        var result = await controller.GetEventEnrolments(s.Ev.EventID);

        TestHelper.AssertStatus(result, 403);
    }

    [Fact]
    public async Task UpdateStatus_ByOwner_ConfirmsTheEnrolment()
    {
        using var db = TestHelper.CreateDb();
        var s = Seed(db);
        var enrolment = TestHelper.AddEnrolment(db, s.Participant.UserID, s.Ev.EventID, s.Cat.CategoryID);
        var controller = TestHelper.WithSession(new EnrolmentsController(db), s.Organiser.UserID, "Organiser");

        var result = await controller.UpdateStatus(enrolment.EnrolmentID, new UpdateEnrolmentStatusRequest { Status = "Confirmed" });

        TestHelper.AssertStatus(result, 200);
        Assert.Equal("Confirmed", db.Enrolments.Single().EnrolmentStatus);
    }
}