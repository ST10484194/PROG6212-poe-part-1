using Microsoft.AspNetCore.Http;
using RaceDay.Auth;
using RaceDay.Controllers;
using RaceDay.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class AuthTests
{
    private static RegisterRequest NewUser(string email = "user@test.com", string role = "Participant") => new()
    {
        FullName = "Test User",
        Email = email,
        Password = "Password123",
        Role = role,
        ContactNumber = "0821234567"
    };

    [Fact]
    public async Task Register_ValidOrganiser_Returns201AndStoresHashedPassword()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Register(NewUser("Organiser@Test.com", "Organiser"));

        TestHelper.AssertStatus(result, 201);
        var user = db.Users.Single();
        Assert.Equal("organiser@test.com", user.Email);
        Assert.Equal(db.Roles.Single(r => r.RoleName == "Organiser").RoleID, user.RoleID);
        Assert.NotEqual("Password123", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123", user.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.WithSession(new AuthController(db));
        await controller.Register(NewUser());

        var result = await controller.Register(NewUser());

        TestHelper.AssertStatus(result, 400);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Register(NewUser(role: "Admin"));

        TestHelper.AssertStatus(result, 400);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200AndStartsSession()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.WithSession(new AuthController(db));
        await controller.Register(NewUser("runner@test.com", "Participant"));

        var result = await controller.Login(new LoginRequest { Email = "runner@test.com", Password = "Password123" });

        TestHelper.AssertStatus(result, 200);
        Assert.NotNull(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Equal("Participant", controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401AndNoSession()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.WithSession(new AuthController(db));
        await controller.Register(NewUser("runner@test.com"));

        var result = await controller.Login(new LoginRequest { Email = "runner@test.com", Password = "WrongPassword" });

        TestHelper.AssertStatus(result, 401);
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.WithSession(new AuthController(db));

        var result = await controller.Login(new LoginRequest { Email = "nobody@test.com", Password = "Password123" });

        TestHelper.AssertStatus(result, 401);
    }

    [Fact]
    public void Logout_ClearsTheSession()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.WithSession(new AuthController(db), userId: 1, role: "Organiser");

        var result = controller.Logout();

        TestHelper.AssertStatus(result, 200);
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.Role));
    }
}