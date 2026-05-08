using EventEase.EMS.Models;

namespace EventEase.EMS.ViewModels;

public class VenueIndexViewModel
{
    public string? Search { get; set; }
    public IReadOnlyList<Venue> Venues { get; set; } = [];
}
