namespace ERP.TenantService.Middleware;
using Microsoft.AspNetCore.Authorization;

public class ApiKeyAuthorizationHandler : AuthorizationHandler<ApiKeyRequirement>
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthorizationHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApiKeyRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var extractedKey)
                && extractedKey == _configuration["ApiKey"])
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}