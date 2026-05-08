using Microsoft.AspNetCore.Http;

namespace EventEase.EMS.Services;

public interface IVenueImageStorage
{
    Task<string?> SaveAsync(IFormFile? file, string? existingPath, CancellationToken cancellationToken = default);
}
