using System.ComponentModel.DataAnnotations;
using EventEase.EMS.Models;

namespace EventEase.EMS.ViewModels;

public class EventFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(140)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(140)]
    [Display(Name = "Client / organiser")]
    public string OrganizerName { get; set; } = string.Empty;

    [StringLength(1200)]
    public string? Description { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Requested start")]
    public DateTime RequestedStartUtc { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Requested end")]
    public DateTime RequestedEndUtc { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Expected guests")]
    public int ExpectedGuests { get; set; }

    public EventStatus Status { get; set; } = EventStatus.PendingVenue;
}
