using webapi.Models;
using webapi.Token;

namespace webapi.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
        Task<RefreshToken> CreateRefreshTokenAsync(Application application);
    }
}
