using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[RequireLogin]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private static readonly Expression<Func<Enrolment, EnrolmentResponse>> ToResponse = en => new EnrolmentResponse
    {
        EnrolmentID = en.EnrolmentID,
        EventID = en.EventID,
        EventName = en.Event!.Name,
        EventDate = en.Event!.EventDate,
        CategoryID = en.CategoryID,
        CategoryName = en.Category!.CategoryName,
        EnrolmentStatus = en.EnrolmentStatus,
        EnrolmentDate = en.EnrolmentDate,
        ParticipantID = en.ParticipantID,
        ParticipantName = en.Participant!.FullName,
        ParticipantEmail = en.Participant!.Email
    };

    /// <summary>Enrols the logged-in Participant into an event under a chosen category.</summary>
    /// <remarks>The new enrolment starts with the status Pending. Past events cannot be entered.</remarks>
    /// <response code="201">The enrolment record with status Pending.</response>
    /// <response code="400">Validation failed or the event has already taken place.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged-in user is not a Participant.</response>
    /// <response code="404">Event, or category for that event, does not exist.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("api/events/{eventId:int}/enrolments")]
    [RequireLogin("Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrolmentRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId && c.EventID == eventId);
        if (category == null)
            return NotFound(new { message = "Category not found for this event." });

        if (ev.EventDate < DateTime.Now)
            return BadRequest(new { message = "This event has already taken place." });

        var userId = CurrentUserId;
        if (await _db.Enrolments.AnyAsync(en => en.ParticipantID == userId && en.EventID == eventId))
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantID = userId,
            EventID = eventId,
            CategoryID = category.CategoryID,
            EnrolmentStatus = "Pending",
            EnrolmentDate = DateTime.Now
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var created = await _db.Enrolments
            .Where(en => en.EnrolmentID == enrolment.EnrolmentID)
            .Select(ToResponse)
            .FirstAsync();

        return CreatedAtAction(nameof(GetMyEnrolments), null, created);
    }

    /// <summary>Returns all of the logged-in Participant's own enrolments.</summary>
    /// <response code="200">Array of enrolments, soonest event first.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged-in user is not a Participant.</response>
    [HttpGet("api/users/me/enrolments")]
    [RequireLogin("Participant")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = CurrentUserId;
        var enrolments = await _db.Enrolments
            .Where(en => en.ParticipantID == userId)
            .OrderBy(en => en.Event!.EventDate)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Returns all Participants enrolled in a specific event, for the owning Organiser.</summary>
    /// <response code="200">Array of enrolments with participant details.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view enrolments for your own events." });

        var enrolments = await _db.Enrolments
            .Where(en => en.EventID == eventId)
            .OrderBy(en => en.EnrolmentDate)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Sets an enrolment's status to Pending or Confirmed (additional endpoint, not in the Part 1 plan).</summary>
    /// <response code="200">The updated enrolment.</response>
    /// <response code="400">Status is not Pending or Confirmed.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpPut("api/enrolments/{id:int}/status")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateEnrolmentStatusRequest request)
    {
        var enrolment = await _db.Enrolments.Include(en => en.Event).FirstOrDefaultAsync(en => en.EnrolmentID == id);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event!.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only manage enrolments for your own events." });

        enrolment.EnrolmentStatus = request.Status;
        await _db.SaveChangesAsync();

        var updated = await _db.Enrolments
            .Where(en => en.EnrolmentID == id)
            .Select(ToResponse)
            .FirstAsync();

        return Ok(updated);
    }
}