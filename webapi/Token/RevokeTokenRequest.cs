namespace webapi.Token
{
    /// <summary>
    /// Revoke token request model.
    /// </summary>
    public class RevokeTokenRequest
    {
        /// <summary>
        /// Refresh token string.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }


}
