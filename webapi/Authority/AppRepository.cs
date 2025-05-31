using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;
using webapi.Security;

namespace webapi.Authority
{
    public static class AppRepository
    {
        // Async fetch from DB by clientId
        public static async Task<Application?> GetApplicationByClientIdAsync(string clientId, ApplicationDbContext db)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            return await db.Applications.FirstOrDefaultAsync(a => a.ClientId == clientId);
        }

        // In-memory fallback apps for demo/testing (precomputed salted hash)
        private static readonly List<Application> _applications;

        // Static constructor to initialize fallback apps with hashed secrets
        static AppRepository()
        {
            // This is the raw secret string clients must use to authenticate (not hashed)
            const string demoSecret = "0673FC70-0514-4011-CCA3-DF9BC03201BC";

            // Hash the secret once for the demo app (returns Base64 salt and hash)
            var (salt, hash) = SecretHasher.HashSecret(demoSecret);

            _applications = new List<Application>
            {
                new Application
                {
                    ApplicationId = 1,
                    ApplicationName = "MVCWebApp",
                    ClientId = "53D3C1E6-5487-8C6E-A8E4BD59940E",
                    SecretSalt = salt,   // Base64-encoded salt
                    SecretHash = hash,   // Base64-encoded hash
                    Scopes = "read,write,delete"
                }
            };
        }

        // Sync fetch from in-memory fallback list
        public static Application? GetApplicationByClientId(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            return _applications.FirstOrDefault(app =>
                app.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));
        }

        // Return all in-memory fallback apps (read-only)
        public static IReadOnlyList<Application> GetAllApplications() => _applications.AsReadOnly();
    }
}
