using System.ComponentModel.DataAnnotations;

namespace EventEase.EMS.Models;

public class Booking
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    [Display(Name = "Booking ID")]
    public string BookingReference { get; set; } = string.Empty;

    [Display(Name = "Event")]
    public int EventRecordId { get; set; }

    [Display(Name = "Venue")]
    public int VenueId { get; set; }

    [Display(Name = "Start date and time")]
    public DateTime StartUtc { get; set; }

    [Display(Name = "End date and time")]
    public DateTime EndUtc { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public EventRecord? EventRecord { get; set; }
    public Venue? Venue { get; set; }
}
