using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Domain;

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
                string? sub = context.Request.Headers["X-User-Id"].FirstOrDefault();

                if (Guid.TryParse(sub, out Guid userId))
                {
                    AuthUser? user = await userRepository.GetByIdAsync(userId);

                    if (user is null)
                    {
                        await WriteErrorAsync(context, 401, "AUTH_019");
                        return; // ✅ stop pipeline
                    }

                    if (!user.CanLogin()) // ✅ covers IsActive + IsDeleted
                    {
                        await WriteErrorAsync(context, 403, "AUTH_003");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static Task WriteErrorAsync(HttpContext context, int statusCode, string code)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new { statusCode, code, message = code });
        }
    }
}