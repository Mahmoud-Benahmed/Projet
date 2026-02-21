using ERP.UserService.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Security;

namespace ERP.UserService.Middleware;

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
            // =========================
            // Application Exceptions
            // =========================
            UserProfileNotFoundException =>
                (404, "USER_001", exception.Message),

            UserProfileAlreadyExistsException =>
                (409, "USER_002", exception.Message),

            // =========================
            // Domain Exceptions
            // =========================
            UserNotActiveException =>
                (403, "USER_003", "User profile is not active"),

            UserActiveException =>
                (403, "USER_004", "User profile is active"),

            InvalidUserProfileException =>
                (400, "USER_005", exception.Message),

            // =========================
            // Security
            // =========================
            UnauthorizedAccessException =>
                (401, "USER_006", exception.Message),

            SecurityException =>
                (401, "USER_007", "Security violation detected"),

            // =========================
            // Fallback
            // =========================
            _ =>
                (500, "SERVER_ERROR", "An unexpected error occurred")
        };

        var problem = new ProblemDetails
        {
            Title = title,
            Status = status,
            Type = $"https://httpstatuses.com/{status}",
            Instance = context.Request.Path
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        return context.Response.WriteAsJsonAsync(problem);
    }
}