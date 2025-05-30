using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using webapi.Data;
using webapi.Token;

namespace webapi.Authority
{
    public class Authenticator : IAuthenticator
    {
        private readonly ApplicationDbContext _db;
        private readonly string _secretKey;
        private readonly ILogger<Authenticator> _logger;

        public Authenticator(ApplicationDbContext db, IConfiguration config, ILogger<Authenticator> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secretKey = config.GetValue<string>("SecretKey")
                         ?? throw new InvalidOperationException("SecretKey is missing in configuration.");
        }

        public async Task<bool> AuthenticateAsync(string clientId, string secret)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
                return false;

            var app = await AppRepository.GetApplicationByClientIdAsync(clientId, _db);
            return app != null && app.Secret == secret;
        }

        public async Task<string> CreateTokenAsync(string clientId, DateTime expiresAt)
        {
            var app = await AppRepository.GetApplicationByClientIdAsync(clientId, _db)
                      ?? throw new ArgumentException("Invalid clientId", nameof(clientId));

            var claims = new List<Claim>
            {
                new Claim("AppName", app.ApplicationName),
            };

            var scopes = app.Scopes?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (scopes != null)
            {
                foreach (var scope in scopes)
                {
                    claims.Add(new Claim(scope.ToLowerInvariant(), "true"));
                }
            }

            var secretKeyBytes = Encoding.ASCII.GetBytes(_secretKey);
            var signingKey = new SymmetricSecurityKey(secretKeyBytes);

            var jwt = new JwtSecurityToken(
                issuer: "WebApi",
                audience: "WebClients",
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public async Task<string> CreateRefreshTokenAsync(string clientId, DateTime expiresAt, HttpContext httpContext)
        {
            var rawToken = Guid.NewGuid().ToString("N");
            var hashedToken = TokenHasher.Hash(rawToken);

            var refresh = new RefreshToken
            {
                Token = hashedToken,
                ClientId = clientId,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                CreatedFromIp = httpContext.Connection.RemoteIpAddress?.ToString(),
                DeviceInfo = httpContext.Request.Headers["User-Agent"].ToString()
            };

            await _db.RefreshTokens.AddAsync(refresh);
            await _db.SaveChangesAsync();

            return rawToken;
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, string clientId)
        {
            var hashed = TokenHasher.Hash(refreshToken);

            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == hashed && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);

            return stored != null && stored.ClientId == clientId;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var hashed = TokenHasher.Hash(refreshToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == hashed);

            if (token == null)
                return false;

            token.IsRevoked = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public IEnumerable<Claim>? VerifyToken(string token, string? secretKey = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = RemoveBearerPrefix(token);

            var key = Encoding.ASCII.GetBytes(secretKey ?? _secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuer = false,
                    ValidateAudience = false
                }, out _);

                return principal?.Claims;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed.");
                return null;
            }
        }

        public IEnumerable<Claim> ReadClaims(string token)
        {
            token = RemoveBearerPrefix(token);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.Claims;
        }

        private static string RemoveBearerPrefix(string token)
        {
            return token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token.Substring("Bearer ".Length).Trim()
                : token;
        }
    }
}
