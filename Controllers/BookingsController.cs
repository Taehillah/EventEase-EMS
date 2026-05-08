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
public class BookingsController(ApplicationDbContext context, IBookingConflictService conflictService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, BookingStatus? status)
    {
        var query = context.Bookings
            .AsNoTracking()
            .Include(b => b.EventRecord)
            .Include(b => b.Venue)
            .OrderByDescending(b => b.StartUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b =>
                b.BookingReference.Contains(search) ||
                (b.EventRecord != null && b.EventRecord.Name.Contains(search)) ||
                (b.Venue != null && b.Venue.Name.Contains(search)));
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var model = new BookingIndexViewModel
        {
            Search = search,
            Status = status,
            Bookings = await query.ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new BookingFormViewModel
        {
            BookingReference = await GenerateBookingReferenceAsync(),
            StartUtc = DateTime.UtcNow.AddDays(7).Date.AddHours(8),
            EndUtc = DateTime.UtcNow.AddDays(7).Date.AddHours(16)
        };

        await PopulateOptionsAsync(model);
        return View("Upsert", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingFormViewModel model)
    {
        await PopulateOptionsAsync(model);
        await ValidateBookingAsync(model, null);

        if (!ModelState.IsValid)
        {
            return View("Upsert", model);
        }

        var booking = new Booking
        {
            BookingReference = model.BookingReference.Trim(),
            EventRecordId = model.EventRecordId,
            VenueId = model.VenueId,
            StartUtc = DateTime.SpecifyKind(model.StartUtc, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(model.EndUtc, DateTimeKind.Utc),
            Status = model.Status
        };

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        await RefreshEventStatusAsync(model.EventRecordId);
        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Booking created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var booking = await context.Bookings.FindAsync(id);
        if (booking is null)
        {
            return NotFound();
        }

        var model = new BookingFormViewModel
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            EventRecordId = booking.EventRecordId,
            VenueId = booking.VenueId,
            StartUtc = booking.StartUtc,
            EndUtc = booking.EndUtc,
            Status = booking.Status
        };

        await PopulateOptionsAsync(model, booking.Id);
        return View("Upsert", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookingFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var booking = await context.Bookings.FindAsync(id);
        if (booking is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model, booking.Id);
        await ValidateBookingAsync(model, booking.Id);

        if (!ModelState.IsValid)
        {
            return View("Upsert", model);
        }

        var previousEventId = booking.EventRecordId;
        booking.BookingReference = model.BookingReference.Trim();
        booking.EventRecordId = model.EventRecordId;
        booking.VenueId = model.VenueId;
        booking.StartUtc = DateTime.SpecifyKind(model.StartUtc, DateTimeKind.Utc);
        booking.EndUtc = DateTime.SpecifyKind(model.EndUtc, DateTimeKind.Utc);
        booking.Status = model.Status;

        await context.SaveChangesAsync();
        await RefreshEventStatusAsync(previousEventId);
        if (previousEventId != model.EventRecordId)
        {
            await RefreshEventStatusAsync(model.EventRecordId);
        }
        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Booking updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await context.Bookings.FindAsync(id);
        if (booking is null)
        {
            return NotFound();
        }

        var eventId = booking.EventRecordId;
        context.Bookings.Remove(booking);
        await context.SaveChangesAsync();
        await RefreshEventStatusAsync(eventId);
        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Booking deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateBookingAsync(BookingFormViewModel model, int? currentBookingId)
    {
        if (model.EndUtc <= model.StartUtc)
        {
            ModelState.AddModelError(nameof(model.EndUtc), "End date must be after the start date.");
        }

        if (!await context.Events.AnyAsync(e => e.Id == model.EventRecordId))
        {
            ModelState.AddModelError(nameof(model.EventRecordId), "Choose a valid event.");
        }

        if (!await context.Venues.AnyAsync(v => v.Id == model.VenueId))
        {
            ModelState.AddModelError(nameof(model.VenueId), "Choose a valid venue.");
        }

        if (await context.Bookings.AnyAsync(b => b.BookingReference == model.BookingReference && b.Id != currentBookingId))
        {
            ModelState.AddModelError(nameof(model.BookingReference), "Booking ID must be unique.");
        }

        if (ModelState.IsValid && await conflictService.HasVenueConflictAsync(model.VenueId, model.StartUtc, model.EndUtc, currentBookingId))
        {
            ModelState.AddModelError(nameof(model.VenueId), "The selected venue already has an active booking on the selected date.");
        }

        if (ModelState.IsValid && await conflictService.HasActiveBookingForEventAsync(model.EventRecordId, currentBookingId))
        {
            ModelState.AddModelError(nameof(model.EventRecordId), "This event already has an active booking.");
        }

        if (ModelState.IsValid)
        {
            var selectedEvent = await context.Events.AsNoTracking().FirstAsync(e => e.Id == model.EventRecordId);
            if (selectedEvent.ExpectedGuests > 0)
            {
                var selectedVenue = await context.Venues.AsNoTracking().FirstAsync(v => v.Id == model.VenueId);
                if (selectedEvent.ExpectedGuests > selectedVenue.Capacity)
                {
                    ModelState.AddModelError(nameof(model.VenueId), "The venue capacity is too small for this event.");
                }
            }
        }
    }

    private async Task PopulateOptionsAsync(BookingFormViewModel model, int? currentBookingId = null)
    {
        var activeEventIds = await context.Bookings
            .Where(b => b.Status != BookingStatus.Cancelled && (!currentBookingId.HasValue || b.Id != currentBookingId.Value))
            .Select(b => b.EventRecordId)
            .ToListAsync();

        model.EventOptions = await context.Events
            .AsNoTracking()
            .Where(e => !activeEventIds.Contains(e.Id) || e.Id == model.EventRecordId)
            .OrderBy(e => e.RequestedStartUtc)
            .Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = $"{e.Name} ({e.RequestedStartUtc:dd MMM yyyy HH:mm})"
            })
            .ToListAsync();

        model.VenueOptions = await context.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.Name} - {v.Location}"
            })
            .ToListAsync();
    }

    private async Task<string> GenerateBookingReferenceAsync()
    {
        var references = await context.Bookings
            .AsNoTracking()
            .Select(b => b.BookingReference)
            .ToListAsync();

        var max = references
            .Select(reference =>
            {
                var digits = new string(reference.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out var parsed) ? parsed : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"BK-{(max + 1):D5}";
    }

    private async Task RefreshEventStatusAsync(int eventId)
    {
        var eventRecord = await context.Events.FindAsync(eventId);
        if (eventRecord is null)
        {
            return;
        }

        var latestActiveBooking = await context.Bookings
            .Where(b => b.EventRecordId == eventId && b.Status != BookingStatus.Cancelled)
            .OrderByDescending(b => b.CreatedUtc)
            .FirstOrDefaultAsync();

        eventRecord.Status = latestActiveBooking is null ? EventStatus.PendingVenue : EventStatus.Scheduled;
        eventRecord.VenueId = latestActiveBooking?.VenueId;
    }
}
