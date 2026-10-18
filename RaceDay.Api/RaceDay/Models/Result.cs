namespace RaceDay.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
    public DateTime RecordedAt { get; set; }

    public Enrolment? Enrolment { get; set; }
}