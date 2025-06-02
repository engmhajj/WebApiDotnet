using webapi.Data;
using webapi.Models;
using webapi.Models.Dtos;
using webapi.Security;

public class ApplicationService
{
    private readonly ApplicationDbContext _db;

    public ApplicationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(Application? app, string? rawSecret)> RegisterApplicationAsync(
        RegisterApplicationDto dto
    )
    {
        // Generate client ID (GUID without dashes)
        var clientId = Guid.NewGuid().ToString("N");

        // Generate a strong random secret - you can replace with stronger secret generation if needed
        var rawSecret = Guid.NewGuid().ToString("N");

        // Hash the secret with salt
        var (salt, hash) = SecretHasher.HashSecret(rawSecret);

        var application = new Application
        {
            ApplicationName = dto.ApplicationName,
            ClientId = clientId,
            SecretSalt = salt,
            SecretHash = hash,
            Scopes = dto.Scopes,
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        // Return application and raw secret to caller
        return (application, rawSecret);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // services.AddDbContext<ApplicationDbContext>(...);
        services.AddScoped<ApplicationService>();
        services.AddScoped<UserService>();
        services.AddControllers();
    }
}
