using EventEase.EMS.Data;
using EventEase.EMS.Models;
using EventEase.EMS.Services;
using EventEase.EMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.EMS.Controllers;

[Authorize]
public class EventsController(ApplicationDbContext context, IBookingConflictService conflictService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, EventStatus? status)
    {
        var query = context.Events
            .AsNoTracking()
            .OrderBy(e => e.RequestedStartUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.Name.Contains(search) || e.OrganizerName.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var model = new EventIndexViewModel
        {
            Search = search,
            Status = status,
            Events = await query.ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Upsert", new EventFormViewModel
        {
            RequestedStartUtc = DateTime.UtcNow.AddDays(7).Date.AddHours(8),
            RequestedEndUtc = DateTime.UtcNow.AddDays(7).Date.AddHours(16)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateDates(model.RequestedStartUtc, model.RequestedEndUtc);

        if (!ModelState.IsValid)
        {
            return View("Upsert", model);
        }

        var eventRecord = new EventRecord
        {
            Name = model.Name.Trim(),
            OrganizerName = model.OrganizerName.Trim(),
            Description = model.Description?.Trim(),
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
            RequestedStartUtc = eventRecord.RequestedStartUtc,
            RequestedEndUtc = eventRecord.RequestedEndUtc,
            ExpectedGuests = eventRecord.ExpectedGuests,
            Status = eventRecord.Status
        };

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

        if (!ModelState.IsValid)
        {
            return View("Upsert", model);
        }

        eventRecord.Name = model.Name.Trim();
        eventRecord.OrganizerName = model.OrganizerName.Trim();
        eventRecord.Description = model.Description?.Trim();
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
}
