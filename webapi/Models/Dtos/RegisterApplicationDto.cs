namespace webapi.Models.Dtos;

public class RegisterApplicationDto
{
    public string ApplicationName { get; set; } = string.Empty;

    // public string ClientId { get; set; } = string.Empty;
    // public string Secret { get; set; } = string.Empty;
    public string Scopes { get; set; } = "read";
}
