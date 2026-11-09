using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Services;

public class BlobImageStorage : IImageStorage
{
    private readonly BlobContainerClient? _container;
    private readonly ILogger<BlobImageStorage> _logger;

    public BlobImageStorage(IConfiguration configuration, ILogger<BlobImageStorage> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = configuration["AzureStorage:ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName)) containerName = "raceday-images";

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                _container = new BlobContainerClient(connectionString, containerName);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                _logger.LogError(ex, "The Azure Storage connection string is not valid.");
            }
        }
    }

    public async Task<string> UploadAsync(Stream content, string contentType, string folder, string extension, CancellationToken cancellationToken = default)
    {
        if (_container == null)
            throw new ImageStorageException("Image storage is not available right now.");

        var blob = _container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");

        try
        {
            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            }, cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Uploading to Azure Blob Storage failed.");
            throw new ImageStorageException("Image storage is not available right now.", ex);
        }

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string? url, string folder, CancellationToken cancellationToken = default)
    {
        if (_container == null || string.IsNullOrWhiteSpace(url)) return;

        var prefix = _container.Uri.AbsoluteUri.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

        var blobName = Uri.UnescapeDataString(url.Substring(prefix.Length));
        if (!blobName.StartsWith(folder + "/", StringComparison.Ordinal)) return;

        try
        {
            await _container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Could not delete the old image {BlobName}.", blobName);
        }
    }
}