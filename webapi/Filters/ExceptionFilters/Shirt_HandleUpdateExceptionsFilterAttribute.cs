using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using webapi.Data;

namespace webapi.Filters.ExceptionFilters;

public class Shirt_HandleUpdateExceptionsFilterAttribute : ExceptionFilterAttribute
{
    private readonly ApplicationDbContext _db;

    public Shirt_HandleUpdateExceptionsFilterAttribute(ApplicationDbContext db)
    {
        _db = db;
    }

    public override void OnException(ExceptionContext context)
    {
        base.OnException(context);

        var routeId = context.RouteData.Values["id"]?.ToString();

        if (int.TryParse(routeId, out int shirtId))
        {
            var shirtStillExists = _db.Shirts.Any(s => s.ShirtId == shirtId);

            if (!shirtStillExists)
            {
                context.ModelState.AddModelError("ShirtId", "Shirt no longer exists.");

                context.Result = new NotFoundObjectResult(
                    new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status404NotFound,
                    }
                );

                context.ExceptionHandled = true; // Mark exception as handled
            }
        }
    }
}
