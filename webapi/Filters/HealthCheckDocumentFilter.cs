using Microsoft.OpenApi.Models;

namespace webapi.Filters;

// Custom document filter to add /health endpoint (optional)
public class HealthCheckDocumentFilter : Swashbuckle.AspNetCore.SwaggerGen.IDocumentFilter
{
    public void Apply(
        OpenApiDocument swaggerDoc,
        Swashbuckle.AspNetCore.SwaggerGen.DocumentFilterContext context
    )
    {
        swaggerDoc.Paths.Add(
            "/health",
            new OpenApiPathItem
            {
                Description = "Health check endpoint",
                Operations =
                {
                    [OperationType.Get] = new OpenApiOperation
                    {
                        Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Health" } },
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse { Description = "Healthy" },
                        },
                    },
                },
            }
        );
    }
}
