using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class EventListViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? EventType { get; set; }

    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType);
}

public class EventDetailViewModel
{
    public EventDto Event { get; set; } = new();
    public EnrolmentDto? MyEnrolment { get; set; }

    public bool HasTakenPlace => Event.EventDate < DateTime.Now;
}

public class MyEnrolmentsViewModel
{
    public List<EnrolmentDto> Upcoming { get; set; } = new();
    public List<EnrolmentDto> Past { get; set; } = new();

    public int Total => Upcoming.Count + Past.Count;
}

public class MyResultsViewModel
{
    public List<ResultDto> Results { get; set; } = new();

    public int Races => Results.Count;
    public int? BestPosition => Results.Count > 0 ? Results.Min(r => r.FinishingPosition) : (int?)null;
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid contact number."), StringLength(20)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }
}