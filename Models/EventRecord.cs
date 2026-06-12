using System.ComponentModel.DataAnnotations;

namespace EventEase.EMS.Models;

public class EventRecord
{
    public int Id { get; set; }

    [Required, StringLength(140)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(140)]
    public string OrganizerName { get; set; } = string.Empty;

    [StringLength(1200)]
    public string? Description { get; set; }

    [Display(Name = "Venue")]
    public int? VenueId { get; set; }

    [Display(Name = "Event type")]
    public int? EventTypeId { get; set; }

    [Display(Name = "Start date and time")]
    public DateTime RequestedStartUtc { get; set; }

    [Display(Name = "End date and time")]
    public DateTime RequestedEndUtc { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Expected guests")]
    public int ExpectedGuests { get; set; }

    public EventStatus Status { get; set; } = EventStatus.PendingVenue;

    public Venue? Venue { get; set; }
    public EventType? EventType { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
