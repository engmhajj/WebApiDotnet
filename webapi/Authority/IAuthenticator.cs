using System.Security.Claims;
using webapi.Models;

namespace webapi.Authority;

public interface IAuthenticator
{
    /// <summary>
    /// Authenticate client credentials asynchronously.
    /// </summary>
    public Task<bool> AuthenticateAsync(string clientId, string secret);

    /// <summary>
    /// Create a JWT access token asynchronously.
    /// </summary>
    public Task<string> CreateTokenAsync(string clientId, DateTime expiresAt);

    public Task<string> CreateRefreshTokenAsync(
        string clientId,
        DateTime expiresAt,
        HttpContext context
    );

    // public Task<TokenResponse> RefreshAccessTokenAsync(string rawRefreshToken, string clientId, HttpContext context);
    /// <summary>
    /// Validate the refresh token asynchronously.
    /// </summary>
    public Task<bool> ValidateRefreshTokenAsync(string refreshToken, string clientId);

    /// <summary>
    /// Revoke a refresh token asynchronously.
    /// </summary>
    public Task<bool> RevokeRefreshTokenAsync(string refreshToken, HttpContext? context = null);

    /// <summary>
    /// Verify a JWT token and get claims.
    /// </summary>
    public IEnumerable<Claim>? VerifyToken(string token, string? secretKey = null);

    /// /// <summary>
    /// /// Read claims from a JWT without validating.
    /// /// </summary>
    /// public IEnumerable<Claim> ReadClaims(string token);
    // Add this:
    public Task<TokenResponse> RefreshAccessTokenAsync(
        string refreshToken,
        string clientId,
        HttpContext context
    );
}
