using System.ComponentModel.DataAnnotations;

namespace webapi.Models;

public class Shirt
{
    public int ShirtId { get; set; }

    [Required]
    public string? Brand { get; set; }

    [Required]
    public string? Color { get; set; }

    public int? Size { get; set; }

    [Required]
    public string? Gender { get; set; }

    public double? Price { get; set; }
    //Version 2
    // public string Description { get; set; } = string.Empty;
    //
    // public bool ValidateDescription()
    // {
    //     return !string.IsNullOrEmpty(Description);
    // }
}
