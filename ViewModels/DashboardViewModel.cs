using EventEase.EMS.Models;

namespace EventEase.EMS.ViewModels;

public class DashboardViewModel
{
    public int TotalBookings { get; set; }
    public int ActiveVenues { get; set; }
    public int UpcomingEvents { get; set; }
    public int PendingBookings { get; set; }
    public IReadOnlyList<Booking> RecentBookings { get; set; } = [];
    public IReadOnlyList<EventRecord> UpcomingEventItems { get; set; } = [];
}
