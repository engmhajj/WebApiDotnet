using Microsoft.EntityFrameworkCore;

using webapi.Db;
using webapi.Models;

namespace webapi.Data
{
	public class ApplicationDbContext : DbContext
	{
		public DbSet<Shirt> Shirts { get; set; }

		public ApplicationDbContext(DbContextOptions options) : base(options)
		{

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			// Utility.CreatePasswordHash("yassmin", out byte[] passwordHash, out byte[] passwordSalt);

			//Data seeding
			modelBuilder.Entity<Shirt>().HasData(new Shirt {
				ShirtId = 1,
				Brand = "hamada",
				Color = "Blue",
				Gender = "Men",
				Price = 30,
				Size = 10,
			},
					 new Shirt {
						 ShirtId = 2,
						 Brand = "My brand",
						 Color = "Black",
						 Gender = "Men",
						 Price = 35,
						 Size = 12,
					 },
				  new Shirt {
					  ShirtId = 3,
					  Brand = "your brand",
					  Color = "Pink",
					  Gender = "Women",
					  Price = 28,
					  Size = 8,
				  },
				  new Shirt {
					  ShirtId = 4,
					  Brand = "your brand",
					  Color = "yello",
					  Gender = "Women",
					  Price = 30,
					  Size = 9,
				  });
		}
	}
}
