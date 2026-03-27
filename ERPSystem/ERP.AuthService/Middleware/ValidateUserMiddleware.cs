using ERP.AuthService.Application.Interfaces.Repositories;
using System.Security.Claims;

namespace ERP.AuthService.Middleware
{
    public class ValidateUserMiddleware
    {
        private readonly RequestDelegate _next;

        public ValidateUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthUserRepository userRepository)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var sub= context.Request.Headers["X-User-Id"].FirstOrDefault();

                if (Guid.TryParse(sub, out var userId))
                {
                    var user = await userRepository.GetByIdAsync(userId);

                    if (user is null)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            statusCode = 401,
                            code = "AUTH_009",
                            content = "Your session is no longer valid. Please log in again."
                        });
                        return;
                    }

                    if (!user.IsActive)
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            statusCode = 403,
                            code = "AUTH_003",
                            content = "Your account has been deactivated. Please contact an administrator."
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}