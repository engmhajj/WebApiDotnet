namespace webapi.Token;

public class RefreshToken
{
    public int RefreshTokenId { get; set; }
    public string Token { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedFromIp { get; set; }
    public string? DeviceInfo { get; set; }
    public bool IsRevoked { get; set; } // Add this!
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }

    public bool IsExpired() => ExpiresAt <= DateTime.UtcNow;
}
