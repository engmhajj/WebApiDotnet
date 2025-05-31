using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using webapi.Authority;
using webapi.Data;
using webapi.Models;
using webapi.Token;

namespace webapi.Services
{
    public class RefreshTokenService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthenticator _authenticator;

        public RefreshTokenService(ApplicationDbContext db, IAuthenticator authenticator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        }

        public async Task<TokenResponse> RefreshAccessTokenAsync(string oldRawRefreshToken, string clientId, HttpContext httpContext)
        {
            var hashed = TokenHasher.Hash(oldRawRefreshToken);

            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == hashed);

            if (stored == null)
                throw new SecurityTokenException("Refresh token not found.");
            if (stored.ClientId != clientId)
                throw new SecurityTokenException("Refresh token does not belong to this client.");
            if (stored.IsRevoked)
                throw new SecurityTokenException("Refresh token is revoked.");
            if (stored.ExpiresAt < DateTime.UtcNow)
                throw new SecurityTokenException("Refresh token is expired.");

            // Revoke old refresh token
            stored.IsRevoked = true;

            // Generate new refresh token
            var newRawRefreshToken = Guid.NewGuid().ToString("N");
            var newHashedRefreshToken = TokenHasher.Hash(newRawRefreshToken);

            string? ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.FirstOrDefault();
            }

            var newRefreshToken = new RefreshToken
            {
                Token = newHashedRefreshToken,
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                CreatedFromIp = ipAddress,
                DeviceInfo = httpContext.Request.Headers["User-Agent"].ToString()
            };

            await _db.RefreshTokens.AddAsync(newRefreshToken);

            // Save revoked + new token changes
            await _db.SaveChangesAsync();

            // Generate new access token
            var newAccessToken = await _authenticator.CreateTokenAsync(clientId, DateTime.UtcNow.AddMinutes(10));

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRawRefreshToken
            };
        }

        public async Task RevokeAsync(string rawToken)
        {
            var hashed = TokenHasher.Hash(rawToken);

            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hashed);
            if (stored != null)
            {
                stored.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }
    }
}
