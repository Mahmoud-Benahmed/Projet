using System.Net;
using System.Text.Json;
using ERP.FournisseurService.Application.Exceptions;

namespace ERP.FournisseurService.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            FournisseurNotFoundException e => (HttpStatusCode.NotFound, e.Message),
            FournisseurBlockedException e => (HttpStatusCode.Conflict, e.Message),
            KeyNotFoundException e => (HttpStatusCode.NotFound, e.Message),
            ArgumentOutOfRangeException e => (HttpStatusCode.BadRequest, e.Message),
            ArgumentException e => (HttpStatusCode.BadRequest, e.Message),
            InvalidOperationException e => (HttpStatusCode.Conflict, e.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var body = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            error = statusCode.ToString(),
            message,
            path = context.Request.Path.Value
        });

        return context.Response.WriteAsync(body);
    }
}