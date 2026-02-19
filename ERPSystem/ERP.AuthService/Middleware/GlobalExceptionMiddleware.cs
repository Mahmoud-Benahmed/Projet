using ERP.AuthService.Application.Exceptions;
using ERP.AuthService.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
using System.Security;
using static System.Net.WebRequestMethods;

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
            var(status, code, title) = exception switch
            {
                EmailAlreadyExistsException => (400, "AUTH_001", "Email already exists"),
                InvalidCredentialsException => (401, "AUTH_002", "Invalid credentials"),
                UserInactiveException => (403, "AUTH_003", "User account is inactive"),
                TokenAlreadyRevokedException => (400, "AUTH_004", "Refresh token already revoked"),
                UnauthorizedAccessException => (401, "AUTH_005", exception.Message),
                SecurityException => (401, "AUTH_006", "Security violation detected"),
                _ => (500, "SERVER_ERROR", "An unexpected error occurred")
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
