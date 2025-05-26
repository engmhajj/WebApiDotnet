using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using webapi.Data;
using webapi.Models;

namespace webapi.Filters.ActionFilter;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class Shirt_ValidateCreateShirtFilterAttribute : ActionFilterAttribute
{
    private readonly ApplicationDbContext _db;

    public Shirt_ValidateCreateShirtFilterAttribute(ApplicationDbContext db)
    {
        _db = db;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        if (context.ActionArguments.TryGetValue("shirt", out var value) && value is Shirt shirt)
        {
            var existingShirt = _db.Shirts.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(shirt.Brand) &&
                !string.IsNullOrWhiteSpace(x.Brand) &&
                x.Brand.ToLower() == shirt.Brand.ToLower() &&

                !string.IsNullOrWhiteSpace(shirt.Gender) &&
                !string.IsNullOrWhiteSpace(x.Gender) &&
                x.Gender.ToLower() == shirt.Gender.ToLower() &&

                !string.IsNullOrWhiteSpace(shirt.Color) &&
                !string.IsNullOrWhiteSpace(x.Color) &&
                x.Color.ToLower() == shirt.Color.ToLower() &&

                shirt.Size.HasValue &&
                x.Size.HasValue &&
                x.Size.Value == shirt.Size.Value
            );

            if (existingShirt != null)
            {
                context.ModelState.AddModelError("Shirt", "Shirt already exists");
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                });
            }
        }
        else
        {
            context.ModelState.AddModelError("Shirt", "Shirt object is null or invalid");
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
            });
        }
    }

    private string GetDebuggerDisplay() => ToString();
}
