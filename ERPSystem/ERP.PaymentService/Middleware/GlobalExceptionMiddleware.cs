// Middleware/GlobalExceptionMiddleware.cs
using ERP.PaymentService.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace ERP.PaymentService.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        (HttpStatusCode statusCode, string message) = exception switch
        {
            // 404 Not Found
            PaymentNotFoundException e =>
                (HttpStatusCode.NotFound, e.Message),

            InvoiceNotFoundException e =>
                (HttpStatusCode.NotFound, e.Message),

            KeyNotFoundException e =>
                (HttpStatusCode.NotFound, e.Message),

            // 409 Conflict
            PaymentAlreadyCancelledException e =>
                (HttpStatusCode.Conflict, e.Message),

            InvoiceAlreadyPaidException e =>
                (HttpStatusCode.Conflict, e.Message),

            InvoiceAlreadyCancelledException e =>
                (HttpStatusCode.Conflict, e.Message),

            // 400 Bad Request
            PaymentDomainException e =>
                (HttpStatusCode.BadRequest, e.Message),

            ArgumentException e =>
                (HttpStatusCode.BadRequest, e.Message),

            InvalidOperationException e =>
                (HttpStatusCode.BadRequest, e.Message),

            // 500 Internal Server Error
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        var response = new
        {
            status = (int)statusCode,
            error = statusCode.ToString(),
            message
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}