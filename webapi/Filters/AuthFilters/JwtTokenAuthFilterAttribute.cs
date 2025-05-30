using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using webapi.Attributes;
using webapi.Authority;

namespace webapi.Filters.AuthFilters;

public class JwtTokenAuthFilter : IAsyncAuthorizationFilter
{
    private readonly Authenticator _authenticator;

    public JwtTokenAuthFilter(Authenticator authenticator)
    {
        _authenticator = authenticator;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrWhiteSpace(authHeader))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var token = authHeader.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var secretKey = config.GetValue<string>("SecretKey");

        var claims = _authenticator.VerifyToken(token, secretKey);
        if (claims == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var requiredClaims = context.ActionDescriptor.EndpointMetadata.OfType<RequiredClaimAttribute>().ToList();
        if (requiredClaims.Any() && !requiredClaims.All(rc => claims.Any(c =>
            string.Equals(c.Type, rc.ClaimType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, rc.ClaimValue, StringComparison.OrdinalIgnoreCase))))
        {
            context.Result = new ForbidResult();
            return;
        }

        await Task.CompletedTask;
    }
}
