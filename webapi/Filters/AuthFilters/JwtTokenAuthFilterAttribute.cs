using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using webapi.Attributes;
using webapi.Authority;

namespace webapi.Filters.AuthFilters;

// This class implements IAsyncAuthorizationFilter, which allows it to intercept HTTP requests before they reach the controller action to perform authorization logic.
public class JwtTokenAuthFilter : IAsyncAuthorizationFilter
{
    private readonly IAuthenticator _authenticator;

    // Takes an Authenticator instance via dependency injection to verify JWT tokens.
    public JwtTokenAuthFilter(IAuthenticator authenticator)
    {
        _authenticator = authenticator;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if the request has an Authorization header
        HttpRequest request = context.HttpContext.Request;

        //Check for Authorization Header
        if (!request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues authHeader) || string.IsNullOrWhiteSpace(authHeader))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        //  Extract and Clean the Token
        string token = authHeader.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
        // If the token is empty or null, return Unauthorized
        if (string.IsNullOrWhiteSpace(token))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        // Retrieve the secret key from configuration
        // Gets the JWT secret key from the app configuration.
        IConfiguration config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        string? secretKey = config.GetValue<string>("SecretKey");

        //Validate Token
        //Uses Authenticator to validate the JWT
        IEnumerable<System.Security.Claims.Claim>? claims = _authenticator.VerifyToken(token, secretKey);
        if (claims == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        //Check Required Claims (Authorization)
        //Retrieves all RequiredClaimAttributes applied on the controller/action.
        //Validates each required claim is present in the JWT claims:
        var requiredClaims = context.ActionDescriptor.EndpointMetadata.OfType<RequiredClaimAttribute>().ToList();
        if (requiredClaims.Count > 0)
        {
            foreach (var rc in requiredClaims)
            {
                // Find the claim by type
                var matchedClaim = claims.FirstOrDefault(c => string.Equals(c.Type, rc.ClaimType, StringComparison.OrdinalIgnoreCase));

                if (matchedClaim is null)
                {
                    context.Result = new ForbidResult();
                    return;
                }

                // Split claim value if it has multiple scopes
                var values = matchedClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // Check if the required value is present
                if (!values.Contains(rc.ClaimValue, StringComparer.OrdinalIgnoreCase))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }

        //A dummy await to satisfy the async signature.
        await Task.CompletedTask;
    }
}
