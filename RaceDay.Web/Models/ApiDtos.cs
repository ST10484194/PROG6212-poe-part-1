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