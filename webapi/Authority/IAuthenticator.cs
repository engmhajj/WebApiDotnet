using System.Security.Claims;

namespace webapi.Authority
{
    public interface IAuthenticator
    {
        /// <summary>
        /// Authenticate client credentials asynchronously.
        /// </summary>
        public Task<bool> AuthenticateAsync(string clientId, string secret);

        /// <summary>
        /// Create a JWT access token asynchronously.
        /// </summary>
        public Task<string> CreateTokenAsync(string clientId, DateTime expiresAt);

        /// <summary>
        /// Create a refresh token asynchronously and persist it.
        /// </summary>
        public Task<string> CreateRefreshTokenAsync(string clientId, DateTime expiresAt, HttpContext httpContext);

        /// <summary>
        /// Validate the refresh token asynchronously.
        /// </summary>
        public Task<bool> ValidateRefreshTokenAsync(string refreshToken, string clientId);

        /// <summary>
        /// Revoke a refresh token asynchronously.
        /// </summary>
        public Task<bool> RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Verify a JWT token and get claims.
        /// </summary>
        public IEnumerable<Claim>? VerifyToken(string token, string? secretKey = null);

        /// <summary>
        /// Read claims from a JWT without validating.
        /// </summary>
        public IEnumerable<Claim> ReadClaims(string token);
    }
}
