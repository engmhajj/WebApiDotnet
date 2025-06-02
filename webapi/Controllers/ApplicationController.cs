using Microsoft.AspNetCore.Mvc;
using webapi.Models.Dtos;

namespace webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationController : ControllerBase
{
    private readonly ApplicationService _service;

    public ApplicationController(ApplicationService service)
    {
        _service = service;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterApplication([FromBody] RegisterApplicationDto dto)
    {
        var (application, secret) = await _service.RegisterApplicationAsync(dto);

        if (application == null)
            return BadRequest(new { message = "Failed to register application." });

        // Return clientId and secret to caller
        return Ok(
            new
            {
                application.ApplicationName,
                application.ClientId,
                Secret = secret, // Return the raw secret here (only once)
                application.Scopes,
            }
        );
    }
}
