using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Registers a new user as an Organiser or a Participant.</summary>
    /// <remarks>Role must be "Organiser" or "Participant". The password is hashed with BCrypt before it is stored.</remarks>
    /// <response code="201">User created. The user record is returned (no password).</response>
    /// <response code="400">Validation failed, role invalid or email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var roleName = request.Role.Trim();
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null)
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        var email = request.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest(new { message = "Email is already registered." });

        var user = new User
        {
            RoleID = role.RoleID,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            ContactNumber = request.ContactNumber,
            CreatedAt = DateTime.Now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(user, role.RoleName));
    }

    /// <summary>Logs a user in and starts a session that stores their UserID and Role.</summary>
    /// <response code="200">Login successful. The user and role are returned.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role!.RoleName);

        return Ok(ToResponse(user, user.Role.RoleName));
    }

    /// <summary>Ends the current user's session.</summary>
    /// <response code="200">Session cleared.</response>
    /// <response code="401">No active session.</response>
    [HttpPost("logout")]
    [RequireLogin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }

    private static UserResponse ToResponse(User user, string roleName) => new()
    {
        UserID = user.UserID,
        FullName = user.FullName,
        Email = user.Email,
        Role = roleName,
        ContactNumber = user.ContactNumber,
        ProfilePictureUrl = user.ProfilePictureUrl
    };
}