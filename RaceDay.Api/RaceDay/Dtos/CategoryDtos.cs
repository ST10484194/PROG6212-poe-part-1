using System.ComponentModel.DataAnnotations;

namespace RaceDay.Dtos;

public class CategoryRequest
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    public int? MaxAge { get; set; }

    [Range(0.01, 1000)]
    public decimal? DistanceKm { get; set; }
}