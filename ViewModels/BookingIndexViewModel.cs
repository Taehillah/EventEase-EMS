using EventEase.EMS.Models;

namespace EventEase.EMS.ViewModels;

public class BookingIndexViewModel
{
    public string? Search { get; set; }
    public BookingStatus? Status { get; set; }
    public IReadOnlyList<Booking> Bookings { get; set; } = [];
}
