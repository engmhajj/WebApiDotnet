using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using webapi.Data;
using webapi.Exceptions;
using webapi.Models;
using webapi.Security;
using webapi.Token;

namespace webapi.Authority;

public class Authenticator : IAuthenticator
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<Authenticator> _logger;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly SymmetricSecurityKey _signingKey;

    public Authenticator(
        ApplicationDbContext db,
        ILogger<Authenticator> logger,
        RefreshTokenService refreshTokenService,
        IOptions<JwtOptions> jwtOptions
    )
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _refreshTokenService =
            refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));

        if (string.IsNullOrWhiteSpace(_jwtOptions.SecretKey))
            throw new InvalidOperationException("SecretKey is missing in JwtOptions.");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
    }

    public async Task<bool> AuthenticateAsync(string clientId, string secret)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("Authentication failed: empty clientId or secret.");
            return false;
        }

        var app = await _db.Applications.FirstOrDefaultAsync(a => a.ClientId == clientId);
        if (app is null)
        {
            _logger.LogWarning("Authentication failed: clientId '{ClientId}' not found.", clientId);
            return false;
        }

        var isValid = SecretHasher.VerifySecret(secret, app.SecretSalt, app.SecretHash);
        if (!isValid)
        {
            _logger.LogWarning(
                "Authentication failed: invalid secret for clientId '{ClientId}'.",
                clientId
            );
        }
        else
        {
            _logger.LogInformation(
                "Authentication successful for clientId '{ClientId}'.",
                clientId
            );
        }
        return isValid;
    }

    public async Task<string> CreateTokenAsync(Application app, DateTime expiresAt)
    {
        if (app is null)
            throw new ArgumentNullException(nameof(app));

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, app.ClientId),
            new Claim("AppName", app.ApplicationName),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        if (!string.IsNullOrWhiteSpace(app.Scopes))
        {
            var scopes = app.Scopes.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            claims.Add(new Claim("scope", string.Join(' ', scopes)));
        }

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Overload to simplify usage when only clientId and expiresAt are available
    public async Task<string> CreateTokenAsync(string clientId, DateTime expiresAt)
    {
        var app =
            await _db.Applications.FirstOrDefaultAsync(a => a.ClientId == clientId)
            ?? throw new ArgumentException("Invalid clientId", nameof(clientId));
        return await CreateTokenAsync(app, expiresAt);
    }

    public Task<string> CreateRefreshTokenAsync(
        string clientId,
        DateTime expiresAt,
        HttpContext context
    )
    {
        return _refreshTokenService.CreateRefreshTokenAsync(clientId, expiresAt, context);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, string clientId)
    {
        var hashed = TokenHasher.Hash(refreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t =>
            t.Token == hashed && !t.IsRevoked && t.ClientId == clientId && !t.IsExpired()
        );

        return token is not null;
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        string refreshToken,
        HttpContext? context = null
    )
    {
        var hashed = TokenHasher.Hash(refreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == hashed);
        if (token is null)
            return false;

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = context?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _db.SaveChangesAsync();
        _logger.LogInformation(
            "Refresh token revoked for clientId '{ClientId}' from IP '{IpAddress}'",
            token.ClientId,
            token.RevokedByIp
        );
        return true;
    }

    public async Task<TokenResponse> RefreshAccessTokenAsync(
        string refreshToken,
        string clientId,
        HttpContext context
    )
    {
        var hashed = TokenHasher.Hash(refreshToken);
        var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(t =>
            t.Token == hashed && !t.IsRevoked && t.ClientId == clientId && !t.IsExpired()
        );

        if (storedToken is null)
        {
            _logger.LogWarning(
                "Invalid or expired refresh token for clientId {ClientId}",
                clientId
            );
            throw new InvalidRefreshTokenException("Invalid or expired refresh token.");
        }

        // Revoke old token with audit info
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _db.SaveChangesAsync();

        var app =
            await _db.Applications.FirstOrDefaultAsync(a => a.ClientId == clientId)
            ?? throw new ArgumentException("Invalid clientId", nameof(clientId));

        var accessExpires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes);
        var refreshExpires = DateTime.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpiryMinutes);

        var newAccessToken = await CreateTokenAsync(app, accessExpires);
        var newRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
            clientId,
            refreshExpires,
            context
        );

        _logger.LogInformation(
            "Access token refreshed for clientId '{ClientId}' from IP '{IpAddress}'",
            clientId,
            storedToken.RevokedByIp
        );

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            ExpiresInSeconds = _jwtOptions.AccessTokenExpiryMinutes * 60,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresInSeconds = _jwtOptions.RefreshTokenExpiryMinutes * 60,
        };
    }

    public IEnumerable<Claim>? VerifyToken(string token, string? overrideSecretKey = null)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        token = RemoveBearerPrefix(token);

        var keyBytes = Encoding.UTF8.GetBytes(overrideSecretKey ?? _jwtOptions.SecretKey);
        var key = new SymmetricSecurityKey(keyBytes);
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuer = !string.IsNullOrEmpty(_jwtOptions.Issuer),
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidateAudience = !string.IsNullOrEmpty(_jwtOptions.Audience),
                    ValidAudience = _jwtOptions.Audience,
                },
                out _
            );

            return principal.Claims;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed.");
            return null;
        }
    }

    public IEnumerable<Claim> ReadClaims(string token)
    {
        token = RemoveBearerPrefix(token);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.Claims;
    }

    private static string RemoveBearerPrefix(string token) =>
        token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token["Bearer ".Length..].Trim()
            : token;
}
