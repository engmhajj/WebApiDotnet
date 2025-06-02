using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using webapi.Data;
using webapi.Token;

public class RefreshTokenService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(ApplicationDbContext db, ILogger<RefreshTokenService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new refresh token for the specified client, stores its hashed value, and returns the plain token.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="expiresAt">The expiration date/time of the token.</param>
    /// <param name="context">The current HTTP context, used to retrieve client IP and device info.</param>
    /// <returns>The plain refresh token string to be sent to the client.</returns>
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

    /// <summary>
    /// Revokes the specified refresh token, marking it revoked and storing revocation audit info if available.
    /// </summary>
    /// <param name="rawToken">The plain refresh token to revoke.</param>
    /// <param name="context">Optional HTTP context to retrieve revocation IP address.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RevokeAsync(string rawToken, HttpContext? context = null)
    {
        var hashed = TokenHasher.Hash(rawToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hashed);

        if (stored != null && !stored.IsRevoked)
        {
            stored.IsRevoked = true;
            stored.RevokedAt = DateTime.UtcNow;
            stored.RevokedByIp = context != null ? GetClientIp(context) ?? "unknown" : "unknown";

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Revoked refresh token for clientId '{ClientId}' from IP '{IpAddress}'",
                stored.ClientId,
                stored.RevokedByIp
            );
        }
        else
        {
            _logger.LogWarning(
                "Attempted to revoke refresh token that does not exist or is already revoked."
            );
        }
    }

    /// <summary>
    /// Extracts the client IP address from the HTTP context, considering proxy headers.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client IP address as a string, or null if not found.</returns>
    private static string? GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var ips = forwarded.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Generates a cryptographically secure, URL-safe random token string.
    /// </summary>
    /// <param name="size">The number of random bytes to generate (default is 64).</param>
    /// <returns>A URL-safe base64-encoded string.</returns>
    private static string GenerateSecureToken(int size = 64)
    {
        var randomBytes = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var base64 = Convert.ToBase64String(randomBytes);
        return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
