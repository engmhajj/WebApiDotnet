using Microsoft.EntityFrameworkCore;
using webapi.Models;
using webapi.Token;

namespace webapi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Application> Applications { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Shirt> Shirts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Application entity config
            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasKey(a => a.ApplicationId);
                entity.HasIndex(a => a.ClientId).IsUnique();
                entity.Property(a => a.ApplicationName).IsRequired();
                entity.Property(a => a.ClientId).IsRequired();
                entity.Property(a => a.Secret).IsRequired();
            });

            // RefreshToken entity config
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(t => t.RefreshTokenId);
                entity.Property(t => t.Token).IsRequired();
                entity.Property(t => t.ClientId).IsRequired();
                entity.Property(t => t.ExpiresAt).IsRequired();
                entity.Property(t => t.CreatedAt).IsRequired();
                entity.Property(t => t.IsRevoked).HasDefaultValue(false);
            });

            // Seed data for initial Application
            modelBuilder.Entity<Application>().HasData(
                new Application
                {
                    ApplicationId = 1,
                    ApplicationName = "MVCWebApp",
                    ClientId = "53D3C1E6-5487-8C6E-A8E4BD59940E",
                    Secret = "0673FC70-0514-4011-CCA3-DF9BC03201BC",
                    Scopes = "read,write,delete"
                }
            );
        }
    }
}
