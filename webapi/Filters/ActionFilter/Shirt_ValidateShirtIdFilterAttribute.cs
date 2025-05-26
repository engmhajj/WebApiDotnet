using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using webapi.Data;

namespace webapi.Filters.ActionFilter;

public class Shirt_ValidateShirtIdFilterAttribute : ActionFilterAttribute
{
    private readonly ApplicationDbContext _db;

    public Shirt_ValidateShirtIdFilterAttribute(ApplicationDbContext db)
    {
        _db = db;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        if (!context.ActionArguments.TryGetValue("id", out var idObj) || idObj is not int shirtId)
        {
            AddErrorAndExit(context, "Shirt Id is missing or invalid.", StatusCodes.Status400BadRequest);
            return;
        }

        if (shirtId <= 0)
        {
            AddErrorAndExit(context, "Shirt Id must be a positive integer.", StatusCodes.Status400BadRequest);
            return;
        }

        var shirt = _db.Shirts.Find(shirtId);
        if (shirt == null)
        {
            AddErrorAndExit(context, "Shirt does not exist.", StatusCodes.Status404NotFound);
            return;
        }

        context.HttpContext.Items["shirt"] = shirt;
    }

    private void AddErrorAndExit(ActionExecutingContext context, string message, int statusCode)
    {
        context.ModelState.AddModelError("shirtId", message);
        context.Result = new ObjectResult(new ValidationProblemDetails(context.ModelState))
        {
            StatusCode = statusCode
        };
    }
}
