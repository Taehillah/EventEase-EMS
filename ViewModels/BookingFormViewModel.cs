using System.ComponentModel.DataAnnotations;
using EventEase.EMS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventEase.EMS.ViewModels;

public class BookingFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(40)]
    [Display(Name = "Booking ID")]
    public string BookingReference { get; set; } = string.Empty;

    [Display(Name = "Event")]
    public int EventRecordId { get; set; }

    [Display(Name = "Venue")]
    public int VenueId { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Start date and time")]
    public DateTime StartUtc { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "End date and time")]
    public DateTime EndUtc { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public IEnumerable<SelectListItem> EventOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> VenueOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
