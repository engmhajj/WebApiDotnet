using System.Data.Entity;
using webapi.Data;
using webapi.Models;
using webapi.Security;

namespace webapi.Authority;

public class AppRepository
{
    private readonly ApplicationDbContext _db;

    public AppRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Application?> GetApplicationByClientIdAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        return await _db.Applications.FirstOrDefaultAsync(a => a.ClientId == clientId);
    }

    public async Task AddApplicationAsync(Application app)
    {
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Application>> GetAllAsync()
    {
        return await _db.Applications.ToListAsync();
    }

    // Optional: dev/test fallback logic (for local testing only)
    private static readonly List<Application> _fallbackApps;

    static AppRepository()
    {
        const string demoSecret = "0673FC70-0514-4011-CCA3-DF9BC03201BC";
        var (salt, hash) = SecretHasher.HashSecret(demoSecret);

        _fallbackApps = new List<Application>
        {
            new Application
            {
                ApplicationId = 1,
                ApplicationName = "MVCWebApp",
                ClientId = "53D3C1E6-5487-8C6E-A8E4BD59940E",
                SecretSalt = salt,
                SecretHash = hash,
                Scopes = "read,write,delete",
            },
        };
    }

    public static Application? GetFallbackApp(string clientId)
    {
        return _fallbackApps.FirstOrDefault(app =>
            app.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)
        );
    }
}
