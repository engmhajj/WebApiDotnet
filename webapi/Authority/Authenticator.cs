using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using webapi.Data;
using webapi.Security;
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

        // Authenticate by verifying the hashed secret matches
        public async Task<bool> AuthenticateAsync(string clientId, string secret)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("Authentication failed: clientId or secret is empty.");
                return false;
            }

            var app = await AppRepository.GetApplicationByClientIdAsync(clientId, _db);
            if (app == null)
            {
                _logger.LogWarning("Authentication failed: clientId '{ClientId}' not found.", clientId);
                return false;
            }

            var isValid = SecretHasher.VerifySecret(secret, app.SecretSalt, app.SecretHash);
            if (!isValid)
            {
                _logger.LogWarning("Authentication failed: invalid secret for clientId '{ClientId}'.", clientId);
            }

            return isValid;
        }

        // Create JWT token for authenticated client
        public async Task<string> CreateTokenAsync(string clientId, DateTime expiresAt)
        {
            var app = await AppRepository.GetApplicationByClientIdAsync(clientId, _db)
                      ?? throw new ArgumentException("Invalid clientId", nameof(clientId));

            var claims = new List<Claim>
            {
                new Claim("AppName", app.ApplicationName)
            };

            var scopes = app.Scopes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (scopes != null && scopes.Any())
            {
                claims.Add(new Claim("scope", string.Join(' ', scopes)));
            }

            var secretKeyBytes = Encoding.UTF8.GetBytes(_secretKey);
            var signingKey = new SymmetricSecurityKey(secretKeyBytes);
            var now = DateTime.UtcNow;

            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

            var jwt = new JwtSecurityToken(
                issuer: "WebApi",
                audience: "WebClients",
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        // Create a new refresh token, hash it and store in DB
        public async Task<string> CreateRefreshTokenAsync(string clientId, DateTime expiresAt, HttpContext httpContext)
        {
            var rawToken = Guid.NewGuid().ToString("N");
            var hashedToken = TokenHasher.Hash(rawToken);

            // Retrieve IP address considering X-Forwarded-For header
            string? ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.FirstOrDefault();
            }

            var refresh = new RefreshToken
            {
                Token = hashedToken,
                ClientId = clientId,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                CreatedFromIp = ipAddress,
                DeviceInfo = httpContext.Request.Headers["User-Agent"].ToString()
            };

            await _db.RefreshTokens.AddAsync(refresh);
            await _db.SaveChangesAsync();

            return rawToken;
        }

        // Validate if refresh token is valid and not revoked
        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, string clientId)
        {
            var hashed = TokenHasher.Hash(refreshToken);

            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == hashed && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);

            return stored != null && stored.ClientId == clientId;
        }

        // Revoke a refresh token by marking it revoked
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

        // Validate JWT token and return claims if valid
        public IEnumerable<Claim>? VerifyToken(string token, string? secretKey = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = RemoveBearerPrefix(token);

            var key = Encoding.UTF8.GetBytes(secretKey ?? _secretKey);
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

        // Read claims from JWT without validating signature
        public IEnumerable<Claim> ReadClaims(string token)
        {
            token = RemoveBearerPrefix(token);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.Claims;
        }

        // Remove "Bearer " prefix from token string
        private static string RemoveBearerPrefix(string token)
        {
            return token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token.Substring("Bearer ".Length).Trim()
                : token;
        }
    }
}
