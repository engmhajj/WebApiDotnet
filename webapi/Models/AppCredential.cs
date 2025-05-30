namespace webapi.Models
{
    /// <summary>
    /// Client credential request model.
    /// </summary>
    public class AppCredential
    {

        /// <summary>
        /// Client identifier.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client secret.
        /// </summary>
        public string Secret { get; set; } = string.Empty;
    }
}
