using EventEase.EMS.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.EMS.Data;

public static class SeedData
{
    public static async Task InitialiseAsync(ApplicationDbContext context)
    {
        var eventTypes = await SeedEventTypesAsync(context);
        await ClassifyExistingEventsAsync(context, eventTypes);

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
                EventTypeId = eventTypes["Gala"],
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
                EventTypeId = eventTypes["Exhibition"],
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
                EventTypeId = eventTypes["Wedding"],
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
                EventTypeId = eventTypes["Corporate"],
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

    private static async Task<Dictionary<string, int>> SeedEventTypesAsync(ApplicationDbContext context)
    {
        string[] predefinedTypes =
        [
            "Conference",
            "Wedding",
            "Concert",
            "Gala",
            "Corporate",
            "Workshop",
            "Exhibition",
            "Private Function"
        ];

        var existingTypes = await context.EventTypes.ToListAsync();
        var existingTypeNames = existingTypes.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingTypes = predefinedTypes
            .Where(typeName => !existingTypeNames.Contains(typeName))
            .Select(typeName => new EventType { Name = typeName })
            .ToList();

        if (missingTypes.Count > 0)
        {
            await context.EventTypes.AddRangeAsync(missingTypes);
            await context.SaveChangesAsync();
        }

        return await context.EventTypes
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Name, t => t.Id);
    }

    private static async Task ClassifyExistingEventsAsync(ApplicationDbContext context, IReadOnlyDictionary<string, int> eventTypes)
    {
        var unclassifiedEvents = await context.Events
            .Where(e => e.EventTypeId == null)
            .ToListAsync();

        if (unclassifiedEvents.Count == 0)
        {
            return;
        }

        foreach (var eventRecord in unclassifiedEvents)
        {
            eventRecord.EventTypeId = GetEventTypeIdForEvent(eventRecord, eventTypes);
        }

        await context.SaveChangesAsync();
    }

    private static int GetEventTypeIdForEvent(EventRecord eventRecord, IReadOnlyDictionary<string, int> eventTypes)
    {
        var searchableText = $"{eventRecord.Name} {eventRecord.Description} {eventRecord.OrganizerName}".ToLowerInvariant();

        if (searchableText.Contains("wedding") || searchableText.Contains("reception"))
        {
            return eventTypes["Wedding"];
        }

        if (searchableText.Contains("summit") || searchableText.Contains("expo") || searchableText.Contains("exhibition"))
        {
            return eventTypes["Exhibition"];
        }

        if (searchableText.Contains("gala") || searchableText.Contains("dinner") || searchableText.Contains("awards"))
        {
            return eventTypes["Gala"];
        }

        if (searchableText.Contains("concert") || searchableText.Contains("festival") || searchableText.Contains("music"))
        {
            return eventTypes["Concert"];
        }

        if (searchableText.Contains("strategy") || searchableText.Contains("corporate") || searchableText.Contains("board"))
        {
            return eventTypes["Corporate"];
        }

        if (searchableText.Contains("workshop") || searchableText.Contains("training"))
        {
            return eventTypes["Workshop"];
        }

        if (searchableText.Contains("conference") || searchableText.Contains("keynote"))
        {
            return eventTypes["Conference"];
        }

        return eventTypes["Private Function"];
    }
}
