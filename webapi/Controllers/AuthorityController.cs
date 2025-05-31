using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using webapi.Authority;
using webapi.Models;
using webapi.Services;
using webapi.Token;

namespace webapi.Controllers
{
    [ApiController]
    [Route("auth")]
    public partial class AuthorityController : ControllerBase
    {
        private readonly ILogger<AuthorityController> _logger;
        private readonly IAuthenticator _authenticator;
        private readonly RefreshTokenService _refreshService;


        private const int AccessTokenExpiryMinutes = 10;
        private const int RefreshTokenExpiryMinutes = 30;
        private const int RefreshTokenExpiryDays = 7;

        /// <summary>
        /// Constructor for AuthorityController.
        /// </summary>
        public AuthorityController(ILogger<AuthorityController> logger, IAuthenticator authenticator, RefreshTokenService refreshService)
        {
            _logger = logger;
            _authenticator = authenticator;
            _refreshService = refreshService;
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
        [ProducesResponseType(typeof(TokenResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _refreshService.RefreshAccessTokenAsync(request.RefreshToken, request.ClientId, HttpContext);
                return Ok(new TokenResponse
                {
                    AccessToken = response.AccessToken,
                    RefreshToken = response.RefreshToken,
                    ExpiresInSeconds = AccessTokenExpiryMinutes * 60,
                    RefreshTokenExpiresInSeconds = RefreshTokenExpiryMinutes * 60 // or 7 days if using longer expiry
                });
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
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

    }
}
