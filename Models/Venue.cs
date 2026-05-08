using System.ComponentModel.DataAnnotations;

namespace EventEase.EMS.Models;

public class Venue
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Location { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int Capacity { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(260)]
    public string? ImagePath { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<EventRecord> Events { get; set; } = new List<EventRecord>();
}
