namespace webapi.Models;

/// <summary>
/// Auth response model for refreshed tokens.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Access token string.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Refresh token string.
    /// </summary>
    public string? RefreshToken { get; set; }
}
