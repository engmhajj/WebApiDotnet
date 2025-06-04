// (*^▽^*)═══════════════════════════════════════════════════════════════════════════════════════════(^▽^*)
// ❖          //Handles storage, revocation, and persistence of refresh tokens in the          ❖
// ❖                                         database.                                         ❖
// (^▽^*)═══════════════════════════════════════════════════════════════════════════════════════════(*^▽^*)
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Token;

namespace webapi.Services;

public class RefreshTokenService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(ApplicationDbContext db, ILogger<RefreshTokenService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(
        string hashedToken,
        string? clientId = null
    )
    {
        if (string.IsNullOrWhiteSpace(hashedToken))
            return null;

        var query = _db.RefreshTokens.AsQueryable();
        if (!string.IsNullOrWhiteSpace(clientId))
            query = query.Where(rt => rt.ClientId == clientId);

        return await query.FirstOrDefaultAsync(rt => rt.Token == hashedToken);
    }

    public async Task<string> CreateRefreshTokenAsync(
        string clientId,
        DateTime expiresAt,
        HttpContext context
    )
    {
        var refreshToken = GenerateSecureToken();
        var hashedToken = TokenHasher.Hash(refreshToken);

        var ip = GetClientIp(context);
        var deviceInfo = context.Request.Headers["User-Agent"].ToString();

        var refreshTokenEntity = new RefreshToken
        {
            Token = hashedToken,
            ClientId = clientId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedFromIp = ip,
            DeviceInfo = deviceInfo,
            IsRevoked = false,
        };

        await _db.RefreshTokens.AddAsync(refreshTokenEntity);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Created refresh token for clientId '{ClientId}' from IP '{IpAddress}'",
            clientId,
            ip
        );
        return refreshToken;
    }

    public async Task<bool> RevokeAsync(string rawToken, HttpContext? context = null)
    {
        var hashed = TokenHasher.Hash(rawToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hashed);

        if (stored == null || stored.IsRevoked)
            return false;

        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedByIp = GetClientIp(context) ?? "unknown";

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Revoked refresh token for clientId '{ClientId}' from IP '{IpAddress}'",
            stored.ClientId,
            stored.RevokedByIp
        );
        return true;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();

    private static string GenerateSecureToken(int size = 64)
    {
        var randomBytes = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var base64 = Convert.ToBase64String(randomBytes);
        return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string? GetClientIp(HttpContext? context)
    {
        if (context?.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) == true)
        {
            var ips = forwarded.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            return ips.Length > 0 ? ips[0].Trim() : null;
        }

        return context?.Connection.RemoteIpAddress?.ToString();
    }
}
