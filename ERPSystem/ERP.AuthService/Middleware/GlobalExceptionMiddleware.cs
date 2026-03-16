using ERP.AuthService.Application.Exceptions;
using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Exceptions.Role;
using Microsoft.AspNetCore.Mvc;
using System.Net;
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
                EmailAlreadyExistsException =>      ((int)HttpStatusCode.Conflict,      "AUTH_001", "Email already exists"),
                InvalidCredentialsException =>      ((int)HttpStatusCode.Unauthorized,  "AUTH_002", exception.Message),
                UserInactiveException =>            ((int)HttpStatusCode.Forbidden,     "AUTH_003", exception.Message),
                UserActiveException =>              ((int)HttpStatusCode.Forbidden,     "AUTH_004", exception.Message),
                TokenAlreadyRevokedException =>     ((int)HttpStatusCode.BadRequest,    "AUTH_005", "Refresh token already revoked"),
                UnauthorizedAccessException =>      ((int)HttpStatusCode.Unauthorized,  "AUTH_006", exception.Message),
                UnauthorizedOperationException =>   ((int)HttpStatusCode.Forbidden,     "AUTH_007", "Operation not authorized"),
                SecurityException =>                ((int)HttpStatusCode.Unauthorized,  "AUTH_008", "Security violation detected"),
                UserNotFoundException =>            ((int)HttpStatusCode.NotFound,      "AUTH_009", exception.Message),
                RoleNotFoundException =>            ((int)HttpStatusCode.NotFound,      "AUTH_010", exception.Message),
                ControleNotFoundException =>        ((int)HttpStatusCode.NotFound,      "AUTH_011", exception.Message),
                PrivilegeNotFoundException =>       ((int)HttpStatusCode.NotFound,      "AUTH_012", exception.Message),
                ArgumentException =>                ((int)HttpStatusCode.BadRequest,    "AUTH_013", exception.Message),
                InvalidOperationException =>        ((int)HttpStatusCode.BadRequest,    "AUTH_014", exception.Message),
                LoginAlreadyExsistException =>      ((int)HttpStatusCode.Conflict,      "AUTH_015", "Login already exists"),
                FluentValidation.ValidationException vex
                                                 => ((int)HttpStatusCode.BadRequest,    "AUTH_016", string.Join(", ", vex.Errors.Select(e => e.ErrorMessage))),
                PwnedPasswordException =>           ((int)HttpStatusCode.BadRequest,    "AUTH_017", exception.Message),
                _ =>                                ((int)HttpStatusCode.InternalServerError, "INTERNAL_ERROR", exception.Message),
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(new { statusCode, code, message });
        }
    }

}
