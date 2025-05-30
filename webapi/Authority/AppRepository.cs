using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;

namespace webapi.Authority
{
    public static class AppRepository
    {
        // Async fetch from DB
        public static async Task<Application?> GetApplicationByClientIdAsync(string clientId, ApplicationDbContext db)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            return await db.Applications.FirstOrDefaultAsync(a => a.ClientId == clientId);
        }

        // Fallback in-memory apps for demo/testing
        private static readonly List<Application> _applications = new()
        {
            new Application
            {
                ApplicationId = 1,
                ApplicationName = "MVCWebApp",
                ClientId = "53D3C1E6-5487-8C6E-A8E4BD59940E",
                Secret = "0673FC70-0514-4011-CCA3-DF9BC03201BC",
                Scopes = "read,write,delete"
            }
        };

        public static Application? GetApplicationByClientId(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            return _applications.FirstOrDefault(app =>
                app.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<Application> GetAllApplications() => _applications.AsReadOnly();
    }
}
