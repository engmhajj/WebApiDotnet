using Microsoft.AspNetCore.Mvc;
using webapi.Models.Dtos;
using webapi.Services;

namespace webapi.Controllers;

[ApiController]
[Route("api/users")]
public class UserAuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;

    public UserAuthController(UserService userService, JwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var result = await _userService.RegisterUserDetailedAsync(dto);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });
        var token = _jwtService.GenerateToken(result.User!);

        return Ok(
            new
            {
                message = "User registered successfully.",
                token,
                user = new
                {
                    result.User!.UserId,
                    result.User.Username,
                    result.User.Email,
                },
            }
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var result = await _userService.AuthenticateUserAsync(dto.Username, dto.Password);
        if (!result.Success || result.User == null)
            return Unauthorized(new { message = result.ErrorMessage ?? "Login failed." });

        var token = _jwtService.GenerateToken(result.User);

        return Ok(
            new
            {
                message = "Login successful.",
                token,
                user = new
                {
                    result.User.UserId,
                    result.User.Username,
                    result.User.Email,
                },
            }
        );
    }
}
