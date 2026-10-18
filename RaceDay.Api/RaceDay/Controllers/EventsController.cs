using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/events")]
[RequireLogin]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private static readonly Expression<Func<Event, EventResponse>> ToResponse = e => new EventResponse
    {
        EventID = e.EventID,
        OrganiserID = e.OrganiserID,
        OrganiserName = e.Organiser!.FullName,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        BannerImageUrl = e.BannerImageUrl,
        CreatedAt = e.CreatedAt,
        EnrolmentCount = e.Enrolments.Count
    };

    /// <summary>Returns all upcoming events, viewable by both roles.</summary>
    /// <remarks>Optional filters: eventType (Run, Walk or Cycle) and search (matches name or location).</remarks>
    /// <response code="200">Array of upcoming events, soonest first.</response>
    /// <response code="401">No active session.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUpcomingEvents([FromQuery] string? eventType, [FromQuery] string? search)
    {
        var query = _db.Events.Where(e => e.EventDate >= DateTime.Now);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search) || e.Location.Contains(search));

        var events = await query.OrderBy(e => e.EventDate).Select(ToResponse).ToListAsync();
        return Ok(events);
    }

    /// <summary>Returns all events created by the logged-in Organiser, including past events.</summary>
    /// <response code="200">Array of the Organiser's events, newest first.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged-in user is not an Organiser.</response>
    [HttpGet("mine")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = CurrentUserId;
        var events = await _db.Events
            .Where(e => e.OrganiserID == userId)
            .OrderByDescending(e => e.EventDate)
            .Select(ToResponse)
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>Returns full detail for a single event, including its categories.</summary>
    /// <response code="200">The event detail.</response>
    /// <response code="401">No active session.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events.Where(e => e.EventID == id).Select(ToResponse).FirstOrDefaultAsync();
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        ev.Categories = await _db.Categories
            .Where(c => c.EventID == id)
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryResponse
            {
                CategoryID = c.CategoryID,
                EventID = c.EventID,
                CategoryName = c.CategoryName,
                MinAge = c.MinAge,
                MaxAge = c.MaxAge,
                DistanceKm = c.DistanceKm
            })
            .ToListAsync();

        return Ok(ev);
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>eventType must be Run, Walk or Cycle.</remarks>
    /// <response code="201">The new event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged-in user is not an Organiser.</response>
    [HttpPost]
    [RequireLogin("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            Name = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            CreatedAt = DateTime.Now
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var created = await _db.Events.Where(e => e.EventID == ev.EventID).Select(ToResponse).FirstAsync();
        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, created);
    }

    /// <summary>Updates an existing event owned by the logged-in Organiser.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id:int}")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only edit your own events." });

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType;

        await _db.SaveChangesAsync();

        var updated = await _db.Events.Where(e => e.EventID == id).Select(ToResponse).FirstAsync();
        return Ok(updated);
    }

    /// <summary>Deletes an event owned by the logged-in Organiser, together with its categories.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">The event has enrolments and cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own events." });

        if (await _db.Enrolments.AnyAsync(en => en.EventID == id))
            return Conflict(new { message = "This event has enrolments and cannot be deleted." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}