using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using webapi.Data;
using webapi.Models;
using webapi.Security;

namespace webapi.Services;

public class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext db,
        IOptions<AppCredential> credentials,
        ILogger logger
    )
    {
        await db.Database.MigrateAsync();

        var appCred = credentials.Value;

        if (
            string.IsNullOrWhiteSpace(appCred.ClientId) || string.IsNullOrWhiteSpace(appCred.Secret)
        )
        {
            logger.LogWarning("ClientId or Secret is missing in AppCredential configuration.");
            return;
        }

        if (!await db.Applications.AnyAsync(a => a.ClientId == appCred.ClientId))
        {
            logger.LogInformation("Seeding default application...");
            var (salt, hash) = SecretHasher.HashSecret(appCred.Secret);

            db.Applications.Add(
                new Application
                {
                    ApplicationName = "DefaultClientApp",
                    ClientId = appCred.ClientId,
                    SecretSalt = salt,
                    SecretHash = hash,
                    Scopes = "read,write",
                }
            );

            await db.SaveChangesAsync();
            logger.LogInformation("Default client seeded successfully.");
        }
        else
        {
            logger.LogInformation("Default client already exists. Skipping seeding.");
        }
    }
}
