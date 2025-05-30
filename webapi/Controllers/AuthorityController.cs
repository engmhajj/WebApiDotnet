using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using webapi.Authority;
using webapi.Models;

namespace webapi.Controllers
{
    [ApiController]
    [Route("auth")]
    public partial class AuthorityController : ControllerBase
    {
        private readonly ILogger<AuthorityController> _logger;
        private readonly IAuthenticator _authenticator;

        private const int AccessTokenExpiryMinutes = 10;
        private const int RefreshTokenExpiryMinutes = 30;
        private const int RefreshTokenExpiryDays = 7;

        /// <summary>
        /// Constructor for AuthorityController.
        /// </summary>
        public AuthorityController(ILogger<AuthorityController> logger, IAuthenticator authenticator)
        {
            _logger = logger;
            _authenticator = authenticator;
        }

        /// <summary>
        /// Obtain access and refresh tokens using client credentials.
        /// </summary>
        /// <param name="credentials">Client credentials</param>
        /// <returns>Access and refresh tokens</returns>
        [HttpPost]
        [SwaggerOperation(Summary = "Obtain access and refresh tokens using client credentials")]
        [ProducesResponseType(typeof(TokenResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetToken([FromBody] AppCredential credentials)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _authenticator.AuthenticateAsync(credentials.ClientId, credentials.Secret))
                return Unauthorized();

            var accessExpires = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes);
            var refreshExpires = DateTime.UtcNow.AddMinutes(RefreshTokenExpiryMinutes);

            var accessToken = await _authenticator.CreateTokenAsync(credentials.ClientId, accessExpires);
            var refreshToken = await _authenticator.CreateRefreshTokenAsync(credentials.ClientId, refreshExpires, HttpContext);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                ExpiresInSeconds = AccessTokenExpiryMinutes * 60,
                RefreshToken = refreshToken,
                RefreshTokenExpiresInSeconds = RefreshTokenExpiryMinutes * 60
            });
        }

        /// <summary>
        /// Refresh access token using a valid refresh token.
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New access token and refresh token</returns>
        [HttpPost("refresh")]
        [SwaggerOperation(Summary = "Refresh access token using a valid refresh token")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _authenticator.ValidateRefreshTokenAsync(request.RefreshToken, request.ClientId))
            {
                _logger.LogWarning("Invalid refresh token for ClientId: {ClientId}", request.ClientId);
                return Unauthorized();
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes);
            var accessToken = await _authenticator.CreateTokenAsync(request.ClientId, expiresAt);
            var newRefreshToken = await _authenticator.CreateRefreshTokenAsync(request.ClientId, DateTime.UtcNow.AddDays(RefreshTokenExpiryDays), HttpContext);

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                RefreshToken = newRefreshToken
            });
        }

        /// <summary>
        /// Revoke a refresh token.
        /// </summary>
        /// <param name="request">Revoke token request</param>
        /// <returns>Status of revocation</returns>
        [HttpPost("revoke")]
        [SwaggerOperation(Summary = "Revoke a refresh token")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _authenticator.RevokeRefreshTokenAsync(request.RefreshToken);

            if (!success)
                return NotFound(new { message = "Token not found." });

            return Ok(new { message = "Refresh token revoked." });
        }

        #region Request and Response DTOs



        /// <summary>
        /// Refresh token request model.
        /// </summary>
        public class RefreshRequest
        {
            /// <summary>
            /// Client identifier.
            /// </summary>
            public string ClientId { get; set; } = string.Empty;

            /// <summary>
            /// Refresh token string.
            /// </summary>
            public string RefreshToken { get; set; } = string.Empty;
        }

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



        #endregion
    }
}
