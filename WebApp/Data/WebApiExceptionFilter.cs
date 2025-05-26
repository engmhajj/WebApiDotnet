using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Data; // Assuming your WebApiException is here

public class WebApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<WebApiExceptionFilter> _logger;

    public WebApiExceptionFilter(ILogger<WebApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is WebApiException webApiEx)
        {
            _logger.LogError(webApiEx, "Web API Exception caught in filter");

            context.Result = new ViewResult
            {
                ViewName = "ErrorServiceUnavailable"
            };

            context.ExceptionHandled = true;
        }
    }
}
