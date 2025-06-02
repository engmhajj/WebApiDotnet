namespace webapi.Models.Dtos;

public class RegisterUserDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Email { get; set; }
    public string? Roles { get; set; }

    // Add this because your validator references ConfirmPassword
    public string? ConfirmPassword { get; set; }
}
