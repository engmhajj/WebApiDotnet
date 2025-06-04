using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;

namespace webapi.Authority
{
    public interface IAppRepository
    {
        Task AddApplicationAsync(Application app);
        Task<List<Application>> GetAllAsync();
        Task<Application?> GetApplicationByClientIdAsync(string clientId);
    }

    public class AppRepository : IAppRepository
    {
        private readonly ApplicationDbContext _db;

        public AppRepository(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<Application?> GetApplicationByClientIdAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            return await _db.Applications.FirstOrDefaultAsync(a => a.ClientId == clientId);
        }

        public async Task AddApplicationAsync(Application app)
        {
            if (app is null)
                throw new ArgumentNullException(nameof(app));

            _db.Applications.Add(app);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Application>> GetAllAsync()
        {
            return await _db.Applications.ToListAsync();
        }
    }
}
