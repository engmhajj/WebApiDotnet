namespace webapi.Models;

public class JwtOptions
{
    public string Issuer { get; set; } = ""; // Add this for your JWT signing key
    public string Audience { get; set; } = ""; // Add this for your JWT signing key
    public string SecretKey { get; set; } = ""; // Add this for your JWT signing key
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryMinutes { get; set; } = 1440;
    public int RefreshTokenExpiryDays { get; set; }
}
