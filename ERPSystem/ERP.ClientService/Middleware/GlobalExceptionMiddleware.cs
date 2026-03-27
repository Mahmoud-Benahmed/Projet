using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace ERP.ClientService.Middleware;

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
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            // ── Specific not-found exceptions first (most specific → least) ───
            ClientNotFoundException ex => new ErrorResponse
            {
                Code = "CLIENT_NOT_FOUND",
                Message = ex.Message,
                StatusCode = (int)HttpStatusCode.NotFound
            },

            CategoryAssignedToUsersException ex => new ErrorResponse
            {
                Code = "CLIENT_DELETE_FAIL",
                Message = ex.Message,
                StatusCode = (int)HttpStatusCode.Conflict
            },

            CategoryNotFoundException ex => new ErrorResponse
            {
                Code = "CATEGORY_NOT_FOUND",
                Message = ex.Message,
                StatusCode = (int)HttpStatusCode.NotFound
            },

            // ── Generic not-found ─────────────────────────────────────────────
            KeyNotFoundException ex => new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                StatusCode = (int)HttpStatusCode.NotFound
            },

            // ── Business rule violations (domain throws InvalidOperationException)
            InvalidOperationException ex => new ErrorResponse
            {
                Code = "BUSINESS_RULE_VIOLATION",
                Message = ex.Message,
                StatusCode = (int)HttpStatusCode.Conflict
            },

            // ── Validation — out-of-range before generic ArgumentException ────
            ArgumentOutOfRangeException ex => new ErrorResponse
            {
                Code = "INVALID_RANGE",
                Message = ex.Message,
                StatusCode = (int)HttpStatusCode.BadRequest
            },

            ArgumentException ex => new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = ex.Message,
                StatusCode = (int)HttpStatusCode.BadRequest
            },

            // ── Database — duplicate key ──────────────────────────────────────
            DbUpdateException ex when IsDuplicateKeyException(ex) => new ErrorResponse
            {
                Code = "DUPLICATE_ENTRY",
                Message = ExtractDuplicateField(ex.InnerException!.Message),
                StatusCode = (int)HttpStatusCode.Conflict
            },

            DbUpdateException => new ErrorResponse
            {
                Code = "DATABASE_ERROR",
                Message = "A database error occurred.",
                StatusCode = (int)HttpStatusCode.InternalServerError
            },

            // ── Fallback ──────────────────────────────────────────────────────
            _ => new ErrorResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = "An unexpected error occurred.",
                StatusCode = (int)HttpStatusCode.InternalServerError
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsDuplicateKeyException(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("unique index", StringComparison.OrdinalIgnoreCase) == true ||
        ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;

    private static string ExtractDuplicateField(string message)
    {
        if (message.Contains("IX_Clients_Email", StringComparison.OrdinalIgnoreCase))
            return "A client with this email already exists.";

        if (message.Contains("IX_Clients_Name", StringComparison.OrdinalIgnoreCase))
            return "A client with this name already exists.";

        if (message.Contains("IX_Categories_Code", StringComparison.OrdinalIgnoreCase))
            return "A category with this code already exists.";

        return "A record with this value already exists.";
    }
}