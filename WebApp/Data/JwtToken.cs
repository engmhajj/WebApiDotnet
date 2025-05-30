using Newtonsoft.Json;

namespace WebApp.Data;

public class JwtToken
{
    [JsonProperty("access_token")]
    public string? AccessToken { get; set; } = string.Empty;

    [JsonProperty("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonProperty("expires_at")]
    public DateTime AccessTokenExpiresAt { get; set; }

    [JsonProperty("refresh_expires_at")]
    public DateTime RefreshTokenExpiresAt { get; set; }

    [JsonIgnore]
    public DateTime IssuedAt { get; set; }
}
