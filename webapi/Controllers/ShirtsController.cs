using Microsoft.AspNetCore.Mvc;
using webapi.Attributes;
using webapi.Data;
using webapi.Filters.ActionFilter;
using webapi.Filters.AuthFilters;
using webapi.Filters.ExceptionFilters;
using webapi.Models;

namespace webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[TypeFilter(typeof(JwtTokenAuthFilter))]
public class ShirtsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ShirtsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [RequiredClaim("read", "true")]
    public IActionResult GetShirts()
    {
        var shirts = _db.Shirts.ToList();
        return Ok(shirts);
    }

    [HttpGet("{id}")]
    [TypeFilter(typeof(Shirt_ValidateShirtIdFilterAttribute))]
    [RequiredClaim("read", "true")]
    public IActionResult GetShirtById(int id)
    {
        var shirt = HttpContext.Items["shirt"] as Shirt;
        return shirt is not null ? Ok(shirt) : NotFound();
    }

    [HttpGet("{id}/{color}")]
    [RequiredClaim("read", "true")]
    public IActionResult GetShirtByIdAndColor(int id, [FromRoute] string color)
    {
        // Consider querying the DB if you want actual filtering instead of just returning a string
        return Ok($"Reading shirt {id} with color {color}");
    }

    [HttpPost]
    [TypeFilter(typeof(Shirt_ValidateCreateShirtFilterAttribute))]
    [RequiredClaim("write", "true")]
    public IActionResult CreateShirtFromForm([FromBody] Shirt shirt)
    {
        _db.Shirts.Add(shirt);
        _db.SaveChanges();

        return CreatedAtAction(nameof(GetShirtById), new { id = shirt.ShirtId }, shirt);
    }

    [HttpPut("{id}")]
    [TypeFilter(typeof(Shirt_ValidateShirtIdFilterAttribute))]
    [Shirt_ValidateUpdateShirtFilter]
    [TypeFilter(typeof(Shirt_HandleUpdateExceptionsFilterAttribute))]
    [RequiredClaim("write", "true")]
    public IActionResult UpdateShirt(int id, [FromBody] Shirt shirt)
    {
        var shirtToUpdate = HttpContext.Items["shirt"] as Shirt;
        if (shirtToUpdate is null)
            return NotFound();

        // Update properties
        shirtToUpdate.Brand = shirt.Brand;
        shirtToUpdate.Color = shirt.Color;
        shirtToUpdate.Price = shirt.Price;
        shirtToUpdate.Size = shirt.Size;
        shirtToUpdate.Gender = shirt.Gender;

        _db.SaveChanges();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [TypeFilter(typeof(Shirt_ValidateShirtIdFilterAttribute))]
    [RequiredClaim("delete", "true")]
    public IActionResult DeleteShirt(int id)
    {
        var shirtToDelete = HttpContext.Items["shirt"] as Shirt;
        if (shirtToDelete is null)
            return NotFound();

        _db.Shirts.Remove(shirtToDelete);
        _db.SaveChanges();

        return Ok(shirtToDelete);
    }
}
