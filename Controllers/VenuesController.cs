using EventEase.EMS.Data;
using EventEase.EMS.Models;
using EventEase.EMS.Services;
using EventEase.EMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.EMS.Controllers;

[Authorize]
public class VenuesController(
    ApplicationDbContext context,
    IVenueImageStorage imageStorage,
    IBookingConflictService conflictService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = context.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v => v.Name.Contains(search) || v.Location.Contains(search));
        }

        var model = new VenueIndexViewModel
        {
            Search = search,
            Venues = await query.ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Upsert", new VenueFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VenueFormViewModel model)
    {
        if (await context.Venues.AnyAsync(v => v.Name == model.Name.Trim()))
        {
            ModelState.AddModelError(nameof(model.Name), "Venue name must be unique.");
        }

        if (!ModelState.IsValid)
        {
            return View("Upsert", model);
        }

        try
        {
            var venue = new Venue
            {
                Name = model.Name.Trim(),
                Location = model.Location.Trim(),
                Capacity = model.Capacity,
                Description = model.Description?.Trim(),
                ImagePath = await imageStorage.SaveAsync(model.ImageFile, null)
            };

            context.Venues.Add(venue);
            await context.SaveChangesAsync();
            TempData["StatusMessage"] = "Venue created.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return View("Upsert", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var venue = await context.Venues.FindAsync(id);
        if (venue is null)
        {
            return NotFound();
        }

        var model = new VenueFormViewModel
        {
            Id = venue.Id,
            Name = venue.Name,
            Location = venue.Location,
            Capacity = venue.Capacity,
            Description = venue.Description,
            ExistingImagePath = venue.ImagePath
        };

        return View("Upsert", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VenueFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var venue = await context.Venues.FindAsync(id);
        if (venue is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.ExistingImagePath = venue.ImagePath;
            return View("Upsert", model);
        }

        if (await context.Venues.AnyAsync(v => v.Name == model.Name.Trim() && v.Id != id))
        {
            ModelState.AddModelError(nameof(model.Name), "Venue name must be unique.");
            model.ExistingImagePath = venue.ImagePath;
            return View("Upsert", model);
        }

        try
        {
            venue.Name = model.Name.Trim();
            venue.Location = model.Location.Trim();
            venue.Capacity = model.Capacity;
            venue.Description = model.Description?.Trim();
            venue.ImagePath = await imageStorage.SaveAsync(model.ImageFile, venue.ImagePath);

            await context.SaveChangesAsync();
            TempData["StatusMessage"] = "Venue updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            model.ExistingImagePath = venue.ImagePath;
            return View("Upsert", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var venue = await context.Venues.FirstOrDefaultAsync(v => v.Id == id);

        if (venue is null)
        {
            return NotFound();
        }

        if (await conflictService.HasActiveBookingForVenueAsync(id))
        {
            TempData["StatusMessage"] = "Alert: this venue has active bookings and cannot be deleted.";
            TempData["StatusMessageType"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        context.Venues.Remove(venue);
        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Venue deleted.";
        return RedirectToAction(nameof(Index));
    }
}
