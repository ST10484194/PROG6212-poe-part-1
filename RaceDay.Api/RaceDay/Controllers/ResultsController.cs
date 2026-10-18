using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Auth;
using RaceDay.Data;
using RaceDay.Dtos;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
[RequireLogin]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private IQueryable<ResultResponse> ToResponses(IQueryable<Result> query) =>
        query.Select(r => new ResultResponse
        {
            ResultID = r.ResultID,
            EnrolmentID = r.EnrolmentID,
            EventID = r.Enrolment!.EventID,
            EventName = r.Enrolment!.Event!.Name,
            EventDate = r.Enrolment!.Event!.EventDate,
            CategoryName = r.Enrolment!.Category!.CategoryName,
            ParticipantID = r.Enrolment!.ParticipantID,
            ParticipantName = r.Enrolment!.Participant!.FullName,
            FinishTime = r.FinishTime,
            FinishingPosition = r.FinishingPosition,
            TotalFinishers = _db.Results.Count(x => x.Enrolment!.EventID == r.Enrolment!.EventID),
            RecordedAt = r.RecordedAt
        });

    /// <summary>Captures the finish time and finishing position for a Participant's enrolment.</summary>
    /// <remarks>Only the Organiser who owns the event can capture results. finishTime is sent as hh:mm:ss, for example 01:45:23. Each enrolment can only have one result.</remarks>
    /// <response code="201">The result record.</response>
    /// <response code="400">Validation failed, or the finish time is not between 00:00:01 and 23:59:59.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Not the owning Organiser.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [RequireLogin("Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(int enrolmentId, [FromBody] ResultRequest request)
    {
        var enrolment = await _db.Enrolments.Include(en => en.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentID == enrolmentId);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event!.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only capture results for your own events." });

        var finishTime = request.FinishTime!.Value;
        if (finishTime <= TimeSpan.Zero || finishTime >= TimeSpan.FromHours(24))
            return BadRequest(new { message = "FinishTime must be between 00:00:01 and 23:59:59." });

        if (await _db.Results.AnyAsync(r => r.EnrolmentID == enrolmentId))
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = finishTime,
            FinishingPosition = request.FinishingPosition,
            RecordedAt = DateTime.Now
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var created = await ToResponses(_db.Results.Where(r => r.ResultID == result.ResultID)).FirstAsync();
        return CreatedAtAction(nameof(GetEventResults), new { eventId = enrolment.EventID }, created);
    }

    /// <summary>Returns the logged-in Participant's full personal race history.</summary>
    /// <remarks>Each result includes the event name, date, category, finish time, finishing position and total finishers (for example 47th out of 312).</remarks>
    /// <response code="200">Array of results, most recent event first.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged-in user is not a Participant.</response>
    [HttpGet("api/users/me/results")]
    [RequireLogin("Participant")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = CurrentUserId;
        var results = await ToResponses(
                _db.Results
                    .Where(r => r.Enrolment!.ParticipantID == userId)
                    .OrderByDescending(r => r.Enrolment!.Event!.EventDate))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Returns all results for a specific event, for publishing a results list.</summary>
    /// <response code="200">Array of results, ordered by finishing position.</response>
    /// <response code="401">No active session.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event not found." });

        var results = await ToResponses(
                _db.Results
                    .Where(r => r.Enrolment!.EventID == eventId)
                    .OrderBy(r => r.FinishingPosition))
            .ToListAsync();

        return Ok(results);
    }
}
