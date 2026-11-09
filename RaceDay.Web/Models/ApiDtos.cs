namespace RaceDay.Web.Models;

/// <summary>A user as returned by the API (never contains a password).</summary>
public class UserDto
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class EventDto
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryDto>? Categories { get; set; }
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string EnrolmentStatus { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
}

public class ResultDto
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