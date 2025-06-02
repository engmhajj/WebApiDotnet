using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using webapi.Authority;
using webapi.Models;
using webapi.Token;

namespace webapi.Controllers;

[ApiController]
[Route("auth")]
public class AuthorityController : ControllerBase
{
    private readonly ILogger<AuthorityController> _logger;
    private readonly IAuthenticator _authenticator;
    private readonly JwtOptions _jwtOptions;
    private readonly AppRepository _appRepository;

    public AuthorityController(
        ILogger<AuthorityController> logger,
        IAuthenticator authenticator,
        IOptions<JwtOptions> jwtOptions,
        AppRepository appRepository
    )
    {
        _logger = logger;
        _authenticator = authenticator;
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _appRepository = appRepository;
    }

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

        var accessExpires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes);
        var refreshExpires = DateTime.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpiryMinutes);

        var accessToken = await _authenticator.CreateTokenAsync(
            credentials.ClientId,
            accessExpires
        );
        var refreshToken = await _authenticator.CreateRefreshTokenAsync(
            credentials.ClientId,
            refreshExpires,
            HttpContext
        );

        return Ok(
            new TokenResponse
            {
                AccessToken = accessToken,
                ExpiresInSeconds = _jwtOptions.AccessTokenExpiryMinutes * 60,
                RefreshToken = refreshToken,
                RefreshTokenExpiresInSeconds = _jwtOptions.RefreshTokenExpiryMinutes * 60,
            }
        );
    }

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
            var response = await _authenticator.RefreshAccessTokenAsync(
                request.RefreshToken,
                request.ClientId,
                HttpContext
            );
            return Ok(response);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed.");
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("revoke")]
    [SwaggerOperation(Summary = "Revoke a refresh token")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var revoked = await _authenticator.RevokeRefreshTokenAsync(request.RefreshToken);
        if (!revoked)
            return BadRequest(new { message = "Refresh token not found or already revoked." });

        return Ok();
    }
}
