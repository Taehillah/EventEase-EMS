using EventEase.EMS.Data;
using EventEase.EMS.Models;
using EventEase.EMS.Services;
using EventEase.EMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEase.EMS.Controllers;

[Authorize]
public class EventsController(ApplicationDbContext context, IBookingConflictService conflictService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        EventStatus? status,
        int? eventTypeId,
        int? venueId,
        DateTime? startDate,
        DateTime? endDate,
        string? availability)
    {
        var query = context.Events
            .AsNoTracking()
            .Include(e => e.EventType)
            .Include(e => e.Venue)
            .Include(e => e.Bookings)
            .OrderBy(e => e.RequestedStartUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Name.Contains(search) ||
                e.OrganizerName.Contains(search) ||
                (e.EventType != null && e.EventType.Name.Contains(search)) ||
                (e.Venue != null && e.Venue.Name.Contains(search)));
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (eventTypeId.HasValue)
        {
            query = query.Where(e => e.EventTypeId == eventTypeId.Value);
        }

        if (venueId.HasValue)
        {
            query = query.Where(e => e.VenueId == venueId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.RequestedEndUtc.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.RequestedStartUtc.Date <= endDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(availability))
        {
            query = availability switch
            {
                "available" => query.Where(e => !e.Bookings.Any(b => b.Status != BookingStatus.Cancelled)),
                "booked" => query.Where(e => e.Bookings.Any(b => b.Status != BookingStatus.Cancelled)),
                "unassigned" => query.Where(e => e.VenueId == null),
                _ => query
            };
        }

        var model = new EventIndexViewModel
        {
            Search = search,
            Status = status,
            EventTypeId = eventTypeId,
            VenueId = venueId,
            StartDate = startDate,
            EndDate = endDate,
            Availability = availability,
            EventTypeOptions = await GetEventTypeOptionsAsync(),
            VenueOptions = await GetVenueOptionsAsync(),
            Events = await query.ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EventFormViewModel
        {
            RequestedStartUtc = DateTime.UtcNow.AddDays(7).Date.AddHours(8),
            RequestedEndUtc = DateTime.UtcNow.AddDays(7).Date.AddHours(16),
            EventTypeOptions = await GetEventTypeOptionsAsync()
        };

        return View("Upsert", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateDates(model.RequestedStartUtc, model.RequestedEndUtc);
        await ValidateEventTypeAsync(model.EventTypeId);

        if (!ModelState.IsValid)
        {
            model.EventTypeOptions = await GetEventTypeOptionsAsync();
            return View("Upsert", model);
        }

        var eventRecord = new EventRecord
        {
            Name = model.Name.Trim(),
            OrganizerName = model.OrganizerName.Trim(),
            Description = model.Description?.Trim(),
            EventTypeId = model.EventTypeId,
            RequestedStartUtc = DateTime.SpecifyKind(model.RequestedStartUtc, DateTimeKind.Utc),
            RequestedEndUtc = DateTime.SpecifyKind(model.RequestedEndUtc, DateTimeKind.Utc),
            ExpectedGuests = model.ExpectedGuests,
            Status = model.Status
        };

        context.Events.Add(eventRecord);
        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Event created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var eventRecord = await context.Events.FindAsync(id);
        if (eventRecord is null)
        {
            return NotFound();
        }

        var model = new EventFormViewModel
        {
            Id = eventRecord.Id,
            Name = eventRecord.Name,
            OrganizerName = eventRecord.OrganizerName,
            Description = eventRecord.Description,
            EventTypeId = eventRecord.EventTypeId,
            RequestedStartUtc = eventRecord.RequestedStartUtc,
            RequestedEndUtc = eventRecord.RequestedEndUtc,
            ExpectedGuests = eventRecord.ExpectedGuests,
            Status = eventRecord.Status
        };

        model.EventTypeOptions = await GetEventTypeOptionsAsync();
        return View("Upsert", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var eventRecord = await context.Events.FindAsync(id);
        if (eventRecord is null)
        {
            return NotFound();
        }

        ValidateDates(model.RequestedStartUtc, model.RequestedEndUtc);
        await ValidateEventTypeAsync(model.EventTypeId);

        if (!ModelState.IsValid)
        {
            model.EventTypeOptions = await GetEventTypeOptionsAsync();
            return View("Upsert", model);
        }

        eventRecord.Name = model.Name.Trim();
        eventRecord.OrganizerName = model.OrganizerName.Trim();
        eventRecord.Description = model.Description?.Trim();
        eventRecord.EventTypeId = model.EventTypeId;
        eventRecord.RequestedStartUtc = DateTime.SpecifyKind(model.RequestedStartUtc, DateTimeKind.Utc);
        eventRecord.RequestedEndUtc = DateTime.SpecifyKind(model.RequestedEndUtc, DateTimeKind.Utc);
        eventRecord.ExpectedGuests = model.ExpectedGuests;
        eventRecord.Status = model.Status;

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Event updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var eventRecord = await context.Events.FirstOrDefaultAsync(e => e.Id == id);

        if (eventRecord is null)
        {
            return NotFound();
        }

        if (await conflictService.HasActiveBookingForEventAsync(id))
        {
            TempData["StatusMessage"] = "Alert: this event has active bookings and cannot be deleted.";
            TempData["StatusMessageType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        context.Events.Remove(eventRecord);
        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Event deleted.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateDates(DateTime startUtc, DateTime endUtc)
    {
        if (endUtc <= startUtc)
        {
            ModelState.AddModelError(nameof(EventFormViewModel.RequestedEndUtc), "End date must be after the start date.");
        }
    }

    private async Task ValidateEventTypeAsync(int? eventTypeId)
    {
        if (!eventTypeId.HasValue || !await context.EventTypes.AnyAsync(t => t.Id == eventTypeId.Value))
        {
            ModelState.AddModelError(nameof(EventFormViewModel.EventTypeId), "Choose a valid event type.");
        }
    }

    private async Task<IReadOnlyList<SelectListItem>> GetEventTypeOptionsAsync()
    {
        return await context.EventTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name
            })
            .ToListAsync();
    }

    private async Task<IReadOnlyList<SelectListItem>> GetVenueOptionsAsync()
    {
        return await context.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = v.Name
            })
            .ToListAsync();
    }
}
