namespace webapi.Models
{

    /// <summary>
    /// Token response model with access and refresh tokens.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// Access token string.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Access token expiration in seconds.
        /// </summary>
        public int ExpiresInSeconds { get; set; }

        /// <summary>
        /// Refresh token string.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token expiration in seconds.
        /// </summary>
        public int RefreshTokenExpiresInSeconds { get; set; }
    }
}
