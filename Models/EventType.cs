using System.ComponentModel.DataAnnotations;

namespace EventEase.EMS.Models;

public class EventType
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public ICollection<EventRecord> Events { get; set; } = new List<EventRecord>();
}
