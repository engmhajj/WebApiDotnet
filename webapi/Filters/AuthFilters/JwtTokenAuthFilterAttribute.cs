using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using webapi.Attributes;
using webapi.Authority;

namespace webapi.Filters.AuthFilters
{
    public class JwtTokenAuthFilterAttribute : Attribute, IAsyncAuthorizationFilter
    {
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

            var configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();
            var secretKey = configuration?.GetValue<string>("SecretKey");

            var claims = Authenticator.VerifyToken(token, secretKey);
            if (claims == null)
            {
                context.Result = new UnauthorizedResult();
            }
            else
            {
                var requireClaims = context.ActionDescriptor.EndpointMetadata.OfType<RequiredClaimAttribute>().ToList();
                if (requireClaims != null && !requireClaims.All(rc => claims.Any(c => c.Type.ToLower() == rc.ClaimType.ToLower() && c.Value.ToLower() == rc.ClaimValue.ToLower())))
                {

                    context.Result = new StatusCodeResult(403);
                }
            }
            await Task.CompletedTask; // To satisfy async method signature
        }
    }
}
