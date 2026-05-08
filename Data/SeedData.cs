using EventEase.EMS.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.EMS.Data;

public static class SeedData
{
    public static async Task InitialiseAsync(ApplicationDbContext context)
    {
        if (await context.Venues.AnyAsync() || await context.Events.AnyAsync() || await context.Bookings.AnyAsync())
        {
            return;
        }

        var venues = new List<Venue>
        {
            new()
            {
                Name = "Grand Ballroom",
                Location = "Sandton Convention Centre, Johannesburg",
                Capacity = 800,
                Description = "Premium ballroom suited to galas, awards nights, and large-scale receptions.",
                ImagePath = "/images/venues/grand-ballroom.jpg"
            },
            new()
            {
                Name = "Conference Hall A",
                Location = "Midrand Business Precinct",
                Capacity = 500,
                Description = "Flexible corporate venue with AV support and breakout facilities.",
                ImagePath = "/images/venues/conference-hall-a.jpg"
            },
            new()
            {
                Name = "Rooftop Garden",
                Location = "Cape Town Waterfront",
                Capacity = 150,
                Description = "Open-air venue suited to weddings, launches, and sunset functions.",
                ImagePath = "/images/venues/rooftop-garden.jpg"
            },
            new()
            {
                Name = "Executive Lounge",
                Location = "Umhlanga Ridge, Durban",
                Capacity = 60,
                Description = "Private space for strategy sessions and board gatherings.",
                ImagePath = "/images/venues/executive-lounge.jpg"
            }
        };

        await context.Venues.AddRangeAsync(venues);
        await context.SaveChangesAsync();

        var events = new List<EventRecord>
        {
            new()
            {
                Name = "Gala Dinner 2026",
                OrganizerName = "Ubuntu Foundation",
                Description = "Annual donor and partner gala.",
                VenueId = venues[0].Id,
                RequestedStartUtc = new DateTime(2026, 3, 24, 17, 0, 0, DateTimeKind.Utc),
                RequestedEndUtc = new DateTime(2026, 3, 24, 22, 0, 0, DateTimeKind.Utc),
                ExpectedGuests = 650,
                Status = EventStatus.Scheduled
            },
            new()
            {
                Name = "Tech Summit 2026",
                OrganizerName = "NovaTech Africa",
                Description = "Regional technology summit with keynote and expo floor.",
                VenueId = venues[1].Id,
                RequestedStartUtc = new DateTime(2026, 3, 22, 7, 30, 0, DateTimeKind.Utc),
                RequestedEndUtc = new DateTime(2026, 3, 22, 16, 0, 0, DateTimeKind.Utc),
                ExpectedGuests = 450,
                Status = EventStatus.Scheduled
            },
            new()
            {
                Name = "Wedding Reception - Du Plessis",
                OrganizerName = "Du Plessis Family",
                Description = "Private evening reception.",
                RequestedStartUtc = new DateTime(2026, 3, 21, 14, 0, 0, DateTimeKind.Utc),
                RequestedEndUtc = new DateTime(2026, 3, 21, 22, 0, 0, DateTimeKind.Utc),
                ExpectedGuests = 140,
                Status = EventStatus.PendingVenue
            },
            new()
            {
                Name = "Corporate Strategy Day",
                OrganizerName = "Blue Peak Holdings",
                Description = "Internal strategic planning session awaiting final venue confirmation.",
                RequestedStartUtc = new DateTime(2026, 3, 25, 6, 0, 0, DateTimeKind.Utc),
                RequestedEndUtc = new DateTime(2026, 3, 25, 14, 0, 0, DateTimeKind.Utc),
                ExpectedGuests = 45,
                Status = EventStatus.PendingVenue
            }
        };

        await context.Events.AddRangeAsync(events);
        await context.SaveChangesAsync();

        var bookings = new List<Booking>
        {
            new()
            {
                BookingReference = "BK-00248",
                EventRecordId = events[0].Id,
                VenueId = venues[0].Id,
                StartUtc = events[0].RequestedStartUtc,
                EndUtc = events[0].RequestedEndUtc,
                Status = BookingStatus.Confirmed
            },
            new()
            {
                BookingReference = "BK-00247",
                EventRecordId = events[1].Id,
                VenueId = venues[1].Id,
                StartUtc = events[1].RequestedStartUtc,
                EndUtc = events[1].RequestedEndUtc,
                Status = BookingStatus.Confirmed
            }
        };

        await context.Bookings.AddRangeAsync(bookings);
        await context.SaveChangesAsync();
    }
}
