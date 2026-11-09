using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

/// <summary>Drives the "choose an event" box at the top of the Organiser pages.</summary>
public class EventSelectorViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public string Controller { get; set; } = string.Empty;
}

public class CategoryFormViewModel
{
    public int? CategoryID { get; set; }
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please enter a category name."), StringLength(50)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 120, ErrorMessage = "The minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "The maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.01, 1000, ErrorMessage = "The distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
}

public class ManageCategoriesViewModel
{
    public EventSelectorViewModel Selector { get; set; } = new();
    public EventDto? Event { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class ManageEnrolmentsViewModel
{
    public EventSelectorViewModel Selector { get; set; } = new();
    public EventDto? Event { get; set; }
    public List<EnrolmentDto> Enrolments { get; set; } = new();

    public int Confirmed => Enrolments.Count(e => e.EnrolmentStatus == "Confirmed");
    public int Pending => Enrolments.Count(e => e.EnrolmentStatus == "Pending");
}

public class ResultRowViewModel
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ManageResultsViewModel
{
    public EventSelectorViewModel Selector { get; set; } = new();
    public EventDto? Event { get; set; }
    public List<ResultRowViewModel> Rows { get; set; } = new();

    public int Captured => Rows.Count(r => r.Result != null);
}

public class CaptureResultViewModel
{
    [Range(1, int.MaxValue)]
    public int EventID { get; set; }

    [Range(1, int.MaxValue)]
    public int EnrolmentID { get; set; }

    [Required(ErrorMessage = "Please enter the finish time as hh:mm:ss, for example 01:45:23.")]
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Enter the finish time as hh:mm:ss, for example 01:45:23.")]
    public string FinishTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the finishing position.")]
    [Range(1, 100000, ErrorMessage = "The finishing position must be 1 or higher.")]
    public int? FinishingPosition { get; set; }
}