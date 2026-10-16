using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests;

public static class TestHelper
{
    /// <summary>A fresh empty database per test (the two roles are seeded by EnsureCreated).</summary>
    public static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>Gives a controller a fake session, optionally already "logged in".</summary>
    public static T WithSession<T>(T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        if (userId.HasValue) httpContext.Session.SetInt32(SessionKeys.UserId, userId.Value);
        if (role != null) httpContext.Session.SetString(SessionKeys.Role, role);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    public static User AddUser(AppDbContext db, string email, string roleName)
    {
        var role = db.Roles.First(r => r.RoleName == roleName);
        var user = new User
        {
            RoleID = role.RoleID,
            FullName = $"Test {roleName}",
            Email = email,
            PasswordHash = "not-a-real-hash",
            CreatedAt = DateTime.Now
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Event AddEvent(AppDbContext db, int organiserId, DateTime? date = null)
    {
        var ev = new Event
        {
            OrganiserID = organiserId,
            Name = "Test Run",
            EventDate = date ?? DateTime.Now.AddDays(30),
            Location = "Thohoyandou",
            DistanceKm = 10,
            EventType = "Run",
            CreatedAt = DateTime.Now
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(AppDbContext db, int eventId, string name = "Senior")
    {
        var category = new Category { EventID = eventId, CategoryName = name, MinAge = 20, MaxAge = 59, DistanceKm = 10 };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(AppDbContext db, int participantId, int eventId, int categoryId, string status = "Pending")
    {
        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            EventID = eventId,
            CategoryID = categoryId,
            EnrolmentStatus = status,
            EnrolmentDate = DateTime.Now
        };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return enrolment;
    }

    /// <summary>Checks the HTTP status code of any controller result.</summary>
    public static void AssertStatus(IActionResult result, int expected)
    {
        int? actual = result switch
        {
            ObjectResult o => o.StatusCode,
            StatusCodeResult s => s.StatusCode,
            _ => null
        };
        Assert.Equal(expected, actual);
    }
}