using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using webapi.Data;
using webapi.Token;

namespace webapi.Token
{
    public static class RefreshTokenStore
    {
        // Save token to DB
        public static async Task SaveAsync(RefreshToken token, ApplicationDbContext db)
        {
            await db.RefreshTokens.AddAsync(token);
            await db.SaveChangesAsync();
        }

        // Get token from DB
        public static async Task<RefreshToken?> GetAsync(string token, ApplicationDbContext db)
        {
            return await db.RefreshTokens
                .Where(t => t.Token == token && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        // Revoke token in DB
        public static async Task RevokeAsync(string token, ApplicationDbContext db)
        {
            var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (existing != null)
            {
                existing.IsRevoked = true;
                await db.SaveChangesAsync();
            }
        }

        // Cleanup expired tokens from DB
        public static async Task CleanupExpiredTokensAsync(ApplicationDbContext db)
        {
            var expiredTokens = db.RefreshTokens.Where(t => t.ExpiresAt <= DateTime.UtcNow);
            db.RefreshTokens.RemoveRange(expiredTokens);
            await db.SaveChangesAsync();
        }
    }
}
