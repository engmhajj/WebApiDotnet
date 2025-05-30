using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using webapi.Authority;
using webapi.Data;
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

        public async Task<string> RefreshAccessTokenAsync(string rawToken, string clientId)
        {
            var hashed = TokenHasher.Hash(rawToken);

            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == hashed && rt.ClientId == clientId);

            if (stored == null || stored.ExpiresAt < DateTime.UtcNow || stored.IsRevoked)
                throw new SecurityTokenException("Invalid or expired refresh token.");

            // Optionally rotate refresh tokens (sliding expiration)
            stored.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
            await _db.SaveChangesAsync();

            // Create new access token using the async CreateTokenAsync method
            return await _authenticator.CreateTokenAsync(clientId, DateTime.UtcNow.AddMinutes(10));
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
