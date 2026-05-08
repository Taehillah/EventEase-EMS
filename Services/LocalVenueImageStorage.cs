using Microsoft.AspNetCore.Http;

namespace EventEase.EMS.Services;

public class LocalVenueImageStorage(IWebHostEnvironment environment) : IVenueImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

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

        var uploadsRoot = Path.Combine(environment.WebRootPath, "uploads", "venues");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        DeleteExisting(existingPath);
        return $"/uploads/venues/{fileName}";
    }

    private void DeleteExisting(string? existingPath)
    {
        if (string.IsNullOrWhiteSpace(existingPath))
        {
            return;
        }

        var relativePath = existingPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(environment.WebRootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
