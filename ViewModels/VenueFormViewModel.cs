using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EventEase.EMS.ViewModels;

public class VenueFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Location { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int Capacity { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? ExistingImagePath { get; set; }

    [Display(Name = "Venue image")]
    public IFormFile? ImageFile { get; set; }
}
