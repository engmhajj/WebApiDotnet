using System.ComponentModel.DataAnnotations;

namespace webapi.Models;

public class User
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string PasswordSalt { get; set; } = null!;

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Roles { get; set; } // Comma-separated or later refactored to separate Role entity

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
