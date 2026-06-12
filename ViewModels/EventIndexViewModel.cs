using EventEase.EMS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventEase.EMS.ViewModels;

public class EventIndexViewModel
{
    public string? Search { get; set; }
    public EventStatus? Status { get; set; }
    public int? EventTypeId { get; set; }
    public int? VenueId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Availability { get; set; }
    public IReadOnlyList<SelectListItem> EventTypeOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> VenueOptions { get; set; } = [];
    public IReadOnlyList<EventRecord> Events { get; set; } = [];
}
