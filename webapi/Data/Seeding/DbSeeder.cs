using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using webapi.Models;
using webapi.Security;

namespace webapi.Data.Seeding;

public class DbSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DbSeeder> _logger;
    private readonly AppCredential _appCredential;

    public DbSeeder(
        ApplicationDbContext db,
        ILogger<DbSeeder> logger,
        IOptions<AppCredential> appCredential
    )
    {
        _db = db;
        _logger = logger;
        _appCredential = appCredential.Value;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Ensuring database is up to date...");
        await _db.Database.MigrateAsync();

        await SeedDefaultClientAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedDefaultClientAsync()
    {
        var existingClient = await _db.Applications.FirstOrDefaultAsync(a =>
            a.ClientId == _appCredential.ClientId
        );
        if (existingClient != null)
        {
            _logger.LogInformation("Default client already exists.");
            return;
        }

        string secret = string.IsNullOrWhiteSpace(_appCredential.Secret)
            ? Guid.NewGuid().ToString("N")
            : _appCredential.Secret;

        var (salt, hash) = SecretHasher.HashSecret(secret);

        var newClient = new Application
        {
            ClientId = _appCredential.ClientId,
            ApplicationName = "MVCWebApp",
            SecretSalt = salt,
            SecretHash = hash,
            Scopes = "read,write,delete",
        };

        _db.Applications.Add(newClient);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded default client.");
        if (string.IsNullOrWhiteSpace(_appCredential.Secret))
            _logger.LogWarning("Generated secret: {Secret}", secret);
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminUsername = "admin";

        if (await _db.Users.AnyAsync(u => u.Username == adminUsername))
        {
            _logger.LogInformation("Admin user already exists.");
            return;
        }

        var (salt, hash) = SecretHasher.HashSecret("admin123");

        _db.Users.Add(
            new User
            {
                Username = adminUsername,
                PasswordHash = hash,
                PasswordSalt = salt, // 👈 This was missing
                Roles = "admin",
                Email = "admin@example.com", // optional
                CreatedAt = DateTime.UtcNow, // optional
            }
        );

        await _db.SaveChangesAsync();
        _logger.LogInformation("Seeded default admin user with username 'admin'.");
    }
}
