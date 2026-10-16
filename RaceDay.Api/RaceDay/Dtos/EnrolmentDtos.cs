using System.ComponentModel.DataAnnotations;

namespace RaceDay.Dtos;

public class EnrolmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    public int CategoryId { get; set; }
}

public class UpdateEnrolmentStatusRequest
{
    [Required, RegularExpression("^(Pending|Confirmed)$", ErrorMessage = "Status must be Pending or Confirmed.")]
    public string Status { get; set; } = string.Empty;
}

public class EnrolmentResponse
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