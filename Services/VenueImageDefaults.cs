namespace EventEase.EMS.Services;

public static class VenueImageDefaults
{
    private const string PlaceholderImage = "/images/venue-placeholder.svg";

    private static readonly Dictionary<string, string> DefaultImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Grand Ballroom"] = "/images/venues/grand-ballroom.jpg",
        ["Conference Hall A"] = "/images/venues/conference-hall-a.jpg",
        ["Rooftop Garden"] = "/images/venues/rooftop-garden.jpg",
        ["Executive Lounge"] = "/images/venues/executive-lounge.jpg"
    };

    public static string? GetDefaultPath(string? venueName)
    {
        if (!string.IsNullOrWhiteSpace(venueName) && DefaultImages.TryGetValue(venueName.Trim(), out var fallbackImage))
        {
            return fallbackImage;
        }

        return null;
    }

    public static string GetDisplayPath(string? imagePath, string? venueName)
    {
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            return imagePath;
        }

        var fallbackPath = GetDefaultPath(venueName);
        if (!string.IsNullOrWhiteSpace(fallbackPath))
        {
            return fallbackPath;
        }

        return PlaceholderImage;
    }
}
