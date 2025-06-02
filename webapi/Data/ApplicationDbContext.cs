using Microsoft.EntityFrameworkCore;
using webapi.Models;
using webapi.Token;

namespace webapi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Application> Applications { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Shirt> Shirts { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //     // Seed Application entity
        // modelBuilder.Entity<Application>().HasData(
        //     new Application
        //     {
        //         ApplicationId = 1,
        //         ApplicationName = "MVCWebApp",
        //         ClientId = "53D3C1E6-5487-8C6E-A8E4BD59940E",
        //         SecretSalt = "BO0dExW/2oK6w8Ns6h6Cmg==",
        //         SecretHash = "FLmgZFaLlSfZ23zLpRA4QZuP5L5G0maULVm+XF/HrJU=",
        //         Scopes = "read,write,delete"
        //     }
        // );
        //
        // // Seed admin user (example, adjust password hash/salt accordingly)
        // modelBuilder.Entity<User>().HasData(
        //     new User
        //     {
        //         UserId = 1,
        //         Username = "admin",
        //         Email = "admin@example.com",
        //         PasswordSalt = "YOUR_GENERATED_SALT_BASE64==",
        //         PasswordHash = "YOUR_GENERATED_HASH_BASE64=="
        //     }
        // );
        //

        // Configure Application entity
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(a => a.ApplicationId);
            entity.HasIndex(a => a.ClientId).IsUnique();
            entity.Property(a => a.ApplicationName).IsRequired();
            entity.Property(a => a.ClientId).IsRequired();
            entity.Property(a => a.SecretHash).IsRequired();
            entity.Property(a => a.SecretSalt).IsRequired();
            entity.Property(a => a.Scopes);
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.RefreshTokenId);
            entity.Property(t => t.Token).IsRequired();
            entity.Property(t => t.ClientId).IsRequired();
            entity.Property(t => t.ExpiresAt).IsRequired();
            entity.Property(t => t.CreatedAt).IsRequired();
            entity.Property(t => t.IsRevoked).HasDefaultValue(false);
        });

        // Optional: Unique index on Username
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
    }
}
