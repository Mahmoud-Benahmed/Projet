using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Exceptions.Role;
using Microsoft.AspNetCore.Mvc;
using System.Security;

namespace ERP.AuthService.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (status, code, title) = exception switch
            {
                EmailAlreadyExistsException => (400, "AUTH_001", "Email already exists"),
                InvalidCredentialsException => (401, "AUTH_002", "Invalid credentials"),
                UserInactiveException => (403, "AUTH_003", "User account is inactive"),
                TokenAlreadyRevokedException => (400, "AUTH_004", "Refresh token already revoked"),
                UnauthorizedAccessException => (401, "AUTH_005", exception.Message),
                UnauthorizedOperationException => (403, "AUTH_006", "Operation not authorized"),
                SecurityException => (401, "AUTH_007", "Security violation detected"),
                UserNotFoundException => (404, "AUTH_008", exception.Message),
                RoleNotFoundException => (404, "AUTH_009", exception.Message),
                ControleNotFoundException => (404, "AUTH_010", exception.Message),
                PrivilegeNotFoundException => (404, "AUTH_011", exception.Message),
                ArgumentException => (400, "AUTH_012", exception.Message),
                InvalidOperationException => (400, "AUTH_013", exception.Message),
                _ => (500, "SERVER_ERROR", exception.Message)
            };

            var problem = new ProblemDetails
            {
                Title = title,
                Status = status,
                Type = $"https://httpstatuses.com/{status}"
            };

            problem.Extensions["code"] = code;
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;
            return context.Response.WriteAsJsonAsync(problem);
        }
    }

}
