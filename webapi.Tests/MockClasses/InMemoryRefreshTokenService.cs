using System.Collections.Concurrent;
using webapi.Interfaces;

public class InMemoryRefreshTokenService : IRefreshTokenService
{
    private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();

    public Task<string> CreateRefreshTokenAsync(
        string clientId,
        DateTime expiresAt,
        HttpContext context
    )
    {
        var token = Guid.NewGuid().ToString();
        var hashed = TokenHasher.Hash(token);

        _tokens[hashed] = new RefreshToken
        {
            Token = hashed,
            ClientId = clientId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = context?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            IsRevoked = false,
        };

        return Task.FromResult(token);
    }

    public Task<bool> RevokeRefreshTokenAsync(string refreshToken, HttpContext? context)
    {
        var hashed = TokenHasher.Hash(refreshToken);
        if (_tokens.TryGetValue(hashed, out var storedToken))
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = context?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<RefreshToken> GetRefreshTokenAsync(string token)
    {
        // Implement logic to retrieve the refresh token
    }

    public Task RevokeRefreshTokenAsync(string token)
    {
        // Implement logic to revoke the refresh token
    }

    public Task<string> CreateRefreshTokenAsync(Application app)
    {
        // Implement logic to create a new refresh token
    }
}
