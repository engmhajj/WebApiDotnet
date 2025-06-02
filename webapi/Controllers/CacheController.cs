namespace webapi.Controllers;

using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

[ApiController]
[Route("[controller]")]
public class CacheController : ControllerBase
{
    private readonly IDistributedCache _cache;

    public CacheController(IDistributedCache cache)
    {
        _cache = cache;
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetValue(string key)
    {
        var value = await _cache.GetStringAsync(key);
        if (value == null)
            return NotFound();
        return Ok(new { key, value });
    }

    [HttpPost("{key}")]
    public async Task<IActionResult> SetValue(string key, [FromBody] string value)
    {
        await _cache.SetStringAsync(
            key,
            value,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            }
        );
        return Ok();
    }

    [HttpPost("enqueue-job")]
    public IActionResult EnqueueJob()
    {
        BackgroundJob.Enqueue(() =>
            Console.WriteLine("Background job executed at " + DateTime.Now)
        );
        return Ok("Job Enqueued");
    }
}
