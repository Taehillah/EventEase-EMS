using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace EventEase.EMS.Services;

public class BlobVenueImageStorage(IConfiguration configuration) : IVenueImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly BlobContainerClient containerClient = CreateContainerClient(configuration);

    public async Task<string?> SaveAsync(IFormFile? file, string? existingPath, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return existingPath;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Venue image must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            throw new InvalidOperationException("Only JPG, PNG, and WEBP venue images are allowed.");
        }

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobName = $"venues/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    }
                },
                cancellationToken);
        }

        await DeleteExistingAsync(existingPath, cancellationToken);
        return blobClient.Uri.ToString();
    }

    private async Task DeleteExistingAsync(string? existingPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(existingPath) || !Uri.TryCreate(existingPath, UriKind.Absolute, out var uri))
        {
            return;
        }

        if (!string.Equals(uri.Host, containerClient.Uri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var blobPrefix = $"{containerClient.Name}/";
        var absolutePath = uri.AbsolutePath.TrimStart('/');
        if (!absolutePath.StartsWith(blobPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var blobName = absolutePath[blobPrefix.Length..];
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return;
        }

        await containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }

    private static BlobContainerClient CreateContainerClient(IConfiguration configuration)
    {
        var connectionString = configuration["VenueImages:StorageConnectionString"];
        var containerName = configuration["VenueImages:ContainerName"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException("Blob storage settings are not configured.");
        }

        return new BlobContainerClient(connectionString, containerName);
    }
}
