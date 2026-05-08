using EventEase.EMS.Models;

namespace EventEase.EMS.ViewModels;

public class EventIndexViewModel
{
    public string? Search { get; set; }
    public EventStatus? Status { get; set; }
    public IReadOnlyList<EventRecord> Events { get; set; } = [];
}
