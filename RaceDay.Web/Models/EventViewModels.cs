using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Models;

public class EventFormViewModel
{
    public int? EventID { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "The description can be at most 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date and start time.")]
    [Display(Name = "Date and start time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.01, 1000, ErrorMessage = "The distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "The event type must be Run, Walk or Cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    /// <summary>The banner already stored for this event (shown as a preview when editing).</summary>
    public string? BannerImageUrl { get; set; }

    /// <summary>A newly chosen banner image. Optional.</summary>
    [Display(Name = "Banner image")]
    public IFormFile? BannerImage { get; set; }
}

public class DashboardViewModel
{
    public List<EventDto> Upcoming { get; set; } = new();
    public List<EventDto> Past { get; set; } = new();
    public int TotalEnrolments { get; set; }

    public int TotalEvents => Upcoming.Count + Past.Count;
    public EventDto? NextEvent => Upcoming.FirstOrDefault();
}