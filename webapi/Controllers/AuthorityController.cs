using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;  // <-- for SwaggerOperation
using webapi.Authority;

namespace webapi.Controllers
{
    [ApiController]
    public class AuthorityController : ControllerBase
    {
        private readonly string _secretKey;
        private readonly ILogger<AuthorityController> _logger;

        public AuthorityController(IConfiguration configuration, ILogger<AuthorityController> logger)
        {
            _secretKey = configuration.GetValue<string>("SecretKey")
                         ?? throw new InvalidOperationException("SecretKey is missing in configuration.");
            _logger = logger;
        }

        /// <summary>
        /// Authenticates an application and returns a JWT token if successful.
        /// </summary>
        /// <param name="credential">The client credentials.</param>
        /// <returns>JWT token and expiration or an error response.</returns>
        [HttpPost("auth")]
        [SwaggerOperation(
            Summary = "Authenticate Application",
            Description = "Validates client credentials and returns a JWT token if successful."
        )]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<AuthResponse> Authenticate([FromBody] AppCredential credential)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Authentication failed due to invalid model state.");
                return ValidationProblem(ModelState);
            }

            if (!Authenticator.Authenticate(credential.ClientId, credential.Secret))
            {
                _logger.LogWarning("Unauthorized access attempt for ClientId: {ClientId}", credential.ClientId);
                ModelState.AddModelError("Unauthorized", "You are not authorized.");
                return Unauthorized(new ValidationProblemDetails(ModelState));
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(10);
            var token = Authenticator.CreateToken(credential.ClientId, expiresAt, _secretKey);

            _logger.LogInformation("Token generated successfully for ClientId: {ClientId}", credential.ClientId);

            var response = new
            {
                access_token = token,
                expiresAt = expiresAt
            };

            return Ok(response);
        }
    }

    /// <summary>
    /// Response object for successful authentication.
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// JWT access token.
        /// </summary>
        public string AccessToken { get; set; } = default!;

        /// <summary>
        /// Token expiration date and time (UTC).
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
