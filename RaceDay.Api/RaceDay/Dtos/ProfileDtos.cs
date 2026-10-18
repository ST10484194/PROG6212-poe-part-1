using System.ComponentModel.DataAnnotations;

namespace RaceDay.Dtos;

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ContactNumber { get; set; }

    [StringLength(255)]
    public string? ProfilePictureUrl { get; set; }
}