using EventEase.EMS.Data;
using EventEase.EMS.Models;
using EventEase.EMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.EMS.Controllers;

[Authorize]
public class DashboardController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        var model = new DashboardViewModel
        {
            TotalBookings = await context.Bookings.CountAsync(),
            ActiveVenues = await context.Venues.CountAsync(),
            UpcomingEvents = await context.Events.CountAsync(e => e.RequestedStartUtc >= now && e.Status != EventStatus.Cancelled),
            PendingBookings = await context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
            RecentBookings = await context.Bookings
                .Include(b => b.EventRecord)
                .Include(b => b.Venue)
                .OrderByDescending(b => b.CreatedUtc)
                .Take(6)
                .ToListAsync(),
            UpcomingEventItems = await context.Events
                .Where(e => e.Status != EventStatus.Cancelled)
                .OrderBy(e => e.RequestedStartUtc)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }
}
