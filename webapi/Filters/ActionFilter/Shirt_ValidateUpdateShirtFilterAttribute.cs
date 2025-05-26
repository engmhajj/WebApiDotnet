using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using webapi.Models;

namespace webapi.Filters.ActionFilter;

public class Shirt_ValidateUpdateShirtFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        // Check if required parameters are provided
        if (!context.ActionArguments.TryGetValue("id", out var idObj) || idObj is not int id)
        {
            context.ModelState.AddModelError("id", "Shirt ID is required.");
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        if (!context.ActionArguments.TryGetValue("shirt", out var shirtObj) || shirtObj is not Shirt shirt)
        {
            context.ModelState.AddModelError("shirt", "Shirt payload is required.");
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        // Validate ID match
        if (shirt.ShirtId != id)
        {
            context.ModelState.AddModelError("ShirtId", "Shirt ID in the route does not match the Shirt ID in the body.");
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
