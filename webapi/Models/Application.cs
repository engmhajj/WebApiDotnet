namespace webapi.Models;

public class Application
{
    public int ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public string? ClientId { get; set; }

    // Remove plain secret to avoid storing it
    // public string? Secret { get; set; }
    public string? SecretSalt { get; set; }
    public string? SecretHash { get; set; }
    public string? Scopes { get; set; }
}
