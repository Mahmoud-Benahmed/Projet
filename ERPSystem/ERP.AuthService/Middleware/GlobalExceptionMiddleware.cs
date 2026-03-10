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
            var (statusCode, code, message) = exception switch
            {
                EmailAlreadyExistsException =>      (400, "AUTH_001", "Email already exists"),
                InvalidCredentialsException =>      (401, "AUTH_002", exception.Message),
                UserInactiveException =>            (403, "AUTH_003", exception.Message),
                UserActiveException =>              (403, "AUTH_004", exception.Message),
                TokenAlreadyRevokedException =>     (400, "AUTH_005", "Refresh token already revoked"),
                UnauthorizedAccessException =>      (401, "AUTH_006", exception.Message),
                UnauthorizedOperationException =>   (403, "AUTH_007", "Operation not authorized"),
                SecurityException =>                (401, "AUTH_008", "Security violation detected"),
                UserNotFoundException =>            (404, "AUTH_009", exception.Message),
                RoleNotFoundException =>            (404, "AUTH_010", exception.Message),
                ControleNotFoundException =>        (404, "AUTH_011", exception.Message),
                PrivilegeNotFoundException =>       (404, "AUTH_012", exception.Message),
                ArgumentException =>                (400, "AUTH_013", exception.Message),
                InvalidOperationException =>        (400, "AUTH_014", exception.Message),
                LoginAlreadyExsistException =>      (400, "AUTH_015", "Login already exists"),
                FluentValidation.ValidationException vex => (400, "AUTH_016", string.Join(", ", vex.Errors.Select(e => e.ErrorMessage))),
                _ =>                                (500, "INTERNAL_ERROR", exception.Message)
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(new { statusCode, code, message });
        }
    }

}
