using Microsoft.AspNetCore.Mvc;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Services;

namespace RaceDay.Controllers;

[ApiController]
[RequireLogin]
public class UploadsController : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IImageStorage _storage;

    public UploadsController(AppDbContext db, IImageStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    /// <summary>Uploads or replaces the banner image for an event, stored in Azure Blob Storage.</summary>
    /// <remarks>Send multipart/form-data with the image in the field "file". Only JPG, PNG or WEBP images up to 5 MB are accepted, and only the Organiser who owns the event can upload.</remarks>
    /// <response code="200">The public URL of the new banner.</response>
    /// <response code="400">No file, a file over 5 MB, or not a JPG, PNG or WEBP image.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="503">Azure Blob Storage is not available.</response>
    [HttpPost("api/events/{id:int}/banner")]
    [RequireLogin("Organiser")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only upload banners for your own events." });

        var image = InspectImage(file);
        if (image.Error != null)
            return BadRequest(new { message = image.Error });

        var folder = $"banners/{id}";
        var oldUrl = ev.BannerImageUrl;

        try
        {
            using var stream = file.OpenReadStream();
            var url = await _storage.UploadAsync(stream, image.ContentType, folder, image.Extension);

            ev.BannerImageUrl = url;
            await _db.SaveChangesAsync();
            await _storage.DeleteAsync(oldUrl, folder);

            return Ok(new { bannerImageUrl = url });
        }
        catch (ImageStorageException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    /// <summary>Uploads or replaces the logged-in user's profile picture, stored in Azure Blob Storage.</summary>
    /// <remarks>Send multipart/form-data with the image in the field "file". Only JPG, PNG or WEBP images up to 5 MB are accepted. Both roles can upload their own picture.</remarks>
    /// <response code="200">The public URL of the new profile picture.</response>
    /// <response code="400">No file, a file over 5 MB, or not a JPG, PNG or WEBP image.</response>
    /// <response code="401">No active session.</response>
    /// <response code="503">Azure Blob Storage is not available.</response>
    [HttpPost("api/users/me/picture")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userId = CurrentUserId;
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "User no longer exists." });

        var image = InspectImage(file);
        if (image.Error != null)
            return BadRequest(new { message = image.Error });

        var folder = $"profiles/{userId}";
        var oldUrl = user.ProfilePictureUrl;

        try
        {
            using var stream = file.OpenReadStream();
            var url = await _storage.UploadAsync(stream, image.ContentType, folder, image.Extension);

            user.ProfilePictureUrl = url;
            await _db.SaveChangesAsync();
            await _storage.DeleteAsync(oldUrl, folder);

            return Ok(new { profilePictureUrl = url });
        }
        catch (ImageStorageException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    /// <summary>Checks the size and the real file signature, so a renamed file cannot pass as an image.</summary>
    private static (string? Error, string ContentType, string Extension) InspectImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return ("Please choose an image file to upload.", "", "");

        if (file.Length > MaxBytes)
            return ("The image must be 5 MB or smaller.", "", "");

        var header = new byte[12];
        int read;
        using (var stream = file.OpenReadStream())
        {
            read = stream.Read(header, 0, header.Length);
        }

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return (null, "image/jpeg", ".jpg");

        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return (null, "image/png", ".png");

        if (read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
            && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
            return (null, "image/webp", ".webp");

        return ("Only JPG, PNG or WEBP images can be uploaded.", "", "");
    }
}