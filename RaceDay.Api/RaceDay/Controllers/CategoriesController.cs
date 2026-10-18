using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[RequireLogin]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private static CategoryResponse ToResponse(Category c) => new()
    {
        CategoryID = c.CategoryID,
        EventID = c.EventID,
        CategoryName = c.CategoryName,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
        DistanceKm = c.DistanceKm
    };

    private static string? ValidateAges(CategoryRequest r) =>
        r.MinAge.HasValue && r.MaxAge.HasValue && r.MinAge > r.MaxAge
            ? "MinAge cannot be greater than MaxAge."
            : null;

    /// <summary>Returns all categories available for a specific event.</summary>
    /// <response code="200">Array of categories.</response>
    /// <response code="401">No active session.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories.Select(ToResponse));
    }

    /// <summary>Adds a new age or distance category to an event.</summary>
    /// <remarks>Only the Organiser who owns the event can add categories. MinAge cannot be greater than MaxAge.</remarks>
    /// <response code="201">The new category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to your own events." });

        var ageError = ValidateAges(request);
        if (ageError != null)
            return BadRequest(new { message = ageError });

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DistanceKm = request.DistanceKm
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { eventId }, ToResponse(category));
    }

    /// <summary>Updates an existing category.</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpPut("api/categories/{id:int}")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit categories of your own events." });

        var ageError = ValidateAges(request);
        if (ageError != null)
            return BadRequest(new { message = ageError });

        category.CategoryName = request.CategoryName.Trim();
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKm = request.DistanceKm;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(category));
    }

    /// <summary>Removes a category from an event.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Participants have enrolled in this category, so it cannot be deleted.</response>
    [HttpDelete("api/categories/{id:int}")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories of your own events." });

        if (await _db.Enrolments.AnyAsync(en => en.CategoryID == id))
            return Conflict(new { message = "Participants have enrolled in this category, so it cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}