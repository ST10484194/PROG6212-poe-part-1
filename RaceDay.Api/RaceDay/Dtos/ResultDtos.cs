using System.ComponentModel.DataAnnotations;

namespace RaceDay.Dtos;

public class ResultRequest
{
    /// <example>01:45:23</example>
    [Required]
    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "FinishingPosition must be 1 or higher.")]
    public int FinishingPosition { get; set; }
}

public class ResultResponse
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
    public int TotalFinishers { get; set; }
    public DateTime RecordedAt { get; set; }
}