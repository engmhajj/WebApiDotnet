// (*^▽^*)══════════════════════════════════════════════════════════════════════════════════════════════════(^▽^*)
// ❖          //Exposes authentication-related endpoints (issue, refresh, revoke) to clients          ❖
// ❖                                            via HTTP.                                             ❖
// (^▽^*)══════════════════════════════════════════════════════════════════════════════════════════════════(*^▽^*)
// NOTE:❗Refactor Note:
//Authenticator no longer manually updates token entities. Instead, it delegates revocation to RefreshTokenService.RevokeAsync(), ensuring all token state management is centralized.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using webapi.Exceptions;
using webapi.Models;
using webapi.Security;
using webapi.Services;
using webapi.Token;

namespace webapi.Authority;

public class Authenticator : IAuthenticator
{
    private readonly IAppRepository _appRepository;
    private readonly IFallbackAppProvider _fallbackAppProvider;
    private readonly ILogger<Authenticator> _logger;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly SymmetricSecurityKey _signingKey;

    public Authenticator(
        IAppRepository appRepository,
        IFallbackAppProvider fallbackAppProvider,
        ILogger<Authenticator> logger,
        RefreshTokenService refreshTokenService,
        IOptions<JwtOptions> jwtOptions
    )
    {
        _appRepository = appRepository ?? throw new ArgumentNullException(nameof(appRepository));
        _fallbackAppProvider =
            fallbackAppProvider ?? throw new ArgumentNullException(nameof(fallbackAppProvider));
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

        var app =
            await _appRepository.GetApplicationByClientIdAsync(clientId)
            ?? _fallbackAppProvider.GetFallbackApp(clientId);

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

    public async Task<string> CreateTokenAsync(string clientId, DateTime expiresAt)
    {
        var app =
            await _appRepository.GetApplicationByClientIdAsync(clientId)
            ?? _fallbackAppProvider.GetFallbackApp(clientId)
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
        var token = await _refreshTokenService.GetRefreshTokenAsync(hashed, clientId);
        return token != null && !token.IsRevoked && !token.IsExpired();
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        string refreshToken,
        HttpContext? context = null
    )
    {
        await _refreshTokenService.RevokeAsync(refreshToken, context);
        // Assuming RevokeAsync handles logging and checking internally
        return true;
    }

    public async Task<TokenResponse> RefreshAccessTokenAsync(
        string refreshToken,
        string clientId,
        HttpContext context
    )
    {
        var hashed = TokenHasher.Hash(refreshToken);
        var storedToken = await _refreshTokenService.GetRefreshTokenAsync(hashed, clientId);

        if (storedToken == null || storedToken.IsRevoked || storedToken.IsExpired())
        {
            _logger.LogWarning(
                "Invalid or expired refresh token for clientId {ClientId}",
                clientId
            );
            throw new InvalidRefreshTokenException("Invalid or expired refresh token.");
        }

        // Revoke old token
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _refreshTokenService.SaveChangesAsync();

        var app =
            await _appRepository.GetApplicationByClientIdAsync(clientId)
            ?? _fallbackAppProvider.GetFallbackApp(clientId)
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
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = key,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            var principal = tokenHandler.ValidateToken(
                token,
                validationParameters,
                out var validatedToken
            );
            return principal.Claims;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed.");
            return null;
        }
    }

    private static string RemoveBearerPrefix(string token)
    {
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return token.Substring("Bearer ".Length).Trim();
        return token;
    }
}
