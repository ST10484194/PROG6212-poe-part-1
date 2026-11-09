namespace RaceDay.Services;

public interface IImageStorage
{
    /// <summary>Uploads an image and returns its public URL.</summary>
    Task<string> UploadAsync(Stream content, string contentType, string folder, string extension, CancellationToken cancellationToken = default);

    /// <summary>Deletes an earlier image. Only blobs inside the given folder are ever deleted.</summary>
    Task DeleteAsync(string? url, string folder, CancellationToken cancellationToken = default);
}

public class ImageStorageException : Exception
{
    public ImageStorageException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}