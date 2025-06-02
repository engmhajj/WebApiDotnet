using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using webapi.Models;

namespace webapi.Filters.ActionFilter.V2;

public class EnsureDescriptionIsPresentFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        // if (context.ActionArguments["shirt"] is Shirt shirt && !shirt.ValidateDescription())
        // {
        //     context.ModelState.AddModelError("Shirt", "Description is required");
        //
        //     var problemDetail = new ValidationProblemDetails(context.ModelState)
        //     {
        //         Status = StatusCodes.Status400BadRequest,
        //     };
        //     context.Result = new BadRequestObjectResult(problemDetail);
        // }
    }
}
