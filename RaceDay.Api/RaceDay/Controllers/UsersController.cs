using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/users")]
[RequireLogin]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns the logged-in user's own profile information.</summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">No active session.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
        if (user == null)
            return Unauthorized(new { message = "User no longer exists." });

        return Ok(ToResponse(user));
    }

    /// <summary>Updates the logged-in user's own profile information.</summary>
    /// <remarks>Send fullName, contactNumber and profilePictureUrl. Email and role cannot be changed.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No active session.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
        if (user == null)
            return Unauthorized(new { message = "User no longer exists." });

        user.FullName = request.FullName.Trim();
        user.ContactNumber = request.ContactNumber;
        user.ProfilePictureUrl = request.ProfilePictureUrl;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    private static UserResponse ToResponse(User user) => new()
    {
        UserID = user.UserID,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role?.RoleName ?? string.Empty,
        ContactNumber = user.ContactNumber,
        ProfilePictureUrl = user.ProfilePictureUrl
    };
}