using EventEase.EMS.Data;
using EventEase.EMS.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.EMS.Services;

public class BookingConflictService(ApplicationDbContext context) : IBookingConflictService
{
    public async Task<bool> HasVenueConflictAsync(int venueId, DateTime startUtc, DateTime endUtc, int? currentBookingId = null)
    {
        var existingBookings = await context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.VenueId == venueId &&
                b.Status != BookingStatus.Cancelled &&
                (!currentBookingId.HasValue || b.Id != currentBookingId.Value))
            .Select(b => new
            {
                b.StartUtc,
                b.EndUtc
            })
            .ToListAsync();

        return existingBookings.Any(b => SharesBookingDate(b.StartUtc, b.EndUtc, startUtc, endUtc));
    }

    public Task<bool> HasActiveBookingForVenueAsync(int venueId)
    {
        return context.Bookings.AnyAsync(b =>
            b.VenueId == venueId &&
            b.Status != BookingStatus.Cancelled);
    }

    public Task<bool> HasActiveBookingForEventAsync(int eventId, int? currentBookingId = null)
    {
        return context.Bookings.AnyAsync(b =>
            b.EventRecordId == eventId &&
            b.Status != BookingStatus.Cancelled &&
            (!currentBookingId.HasValue || b.Id != currentBookingId.Value));
    }

    private static bool SharesBookingDate(DateTime firstStartUtc, DateTime firstEndUtc, DateTime secondStartUtc, DateTime secondEndUtc)
    {
        var firstStartDate = firstStartUtc.Date;
        var secondStartDate = secondStartUtc.Date;
        var firstEndDateExclusive = GetDateRangeEndExclusive(firstEndUtc);
        var secondEndDateExclusive = GetDateRangeEndExclusive(secondEndUtc);

        return firstStartDate < secondEndDateExclusive && secondStartDate < firstEndDateExclusive;
    }

    private static DateTime GetDateRangeEndExclusive(DateTime endUtc)
    {
        return endUtc.TimeOfDay == TimeSpan.Zero
            ? endUtc.Date
            : endUtc.Date.AddDays(1);
    }
}
