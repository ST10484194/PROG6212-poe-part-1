namespace RaceDay.Web.Services;

/// <summary>Quick checks before an image is sent to the API. The API checks the file again.</summary>
public static class ImageValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "Please choose an image to upload.";

        if (file.Length > MaxBytes)
            return "The image must be 5 MB or smaller.";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return "Only JPG, PNG or WEBP images can be uploaded.";

        return null;
    }
}