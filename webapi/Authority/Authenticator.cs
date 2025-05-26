using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace webapi.Authority;

public static class Authenticator
{
    public static bool Authenticate(string clientId, string secret)
    {
        var app = AppRepository.GetApplicationByClientId(clientId);
        return app != null && app.Secret == secret;
    }

    public static string CreateToken(string clientId, DateTime expiresAt, string strSecretKey)
    {
        var app = AppRepository.GetApplicationByClientId(clientId)
            ?? throw new ArgumentException("Invalid clientId", nameof(clientId));

        var claims = new List<Claim>
        {
            new Claim("AppName", app.ApplicationName),
        };

        var scopes = app.Scopes?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (scopes is not null)
        {
            foreach (var scope in scopes)
            {
                claims.Add(new Claim(scope.ToLowerInvariant(), "true"));
            }
        }

        var secretKey = Encoding.ASCII.GetBytes(strSecretKey);
        var signingKey = new SymmetricSecurityKey(secretKey);

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

    public static IEnumerable<Claim>? VerifyToken(string token, string strSecretKey)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        token = RemoveBearerPrefix(token);


        var secretKey = Encoding.ASCII.GetBytes(strSecretKey);
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuer = false,
                ValidateAudience = false
            }, out _);

            return principal?.Claims;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception("Unexpected error during token verification.", ex);
        }
    }

    public static IEnumerable<Claim> ReadClaims(string token)
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
