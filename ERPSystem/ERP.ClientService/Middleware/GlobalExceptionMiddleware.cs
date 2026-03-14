using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace ERP.ClientService.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = exception switch
            {
                ClientNotFoundException ex => new ErrorResponse
                {
                    Code = "CLIENT_NOT_FOUND",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.NotFound
                },

                KeyNotFoundException ex => new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.NotFound
                },

                ArgumentException ex => new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                DbUpdateException ex when ex.InnerException?.Message.Contains("unique index") == true ||
                                          ex.InnerException?.Message.Contains("duplicate key") == true => new ErrorResponse
                                          {
                                              Code = "DUPLICATE_ENTRY",
                                              Message = ExtractDuplicateField(ex.InnerException!.Message),
                                              StatusCode = (int)HttpStatusCode.Conflict
                                          },

                DbUpdateException ex => new ErrorResponse
                {
                    Code = "DATABASE_ERROR",
                    Message = "A database error occurred.",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                },

                _ => new ErrorResponse
                {
                    Code = "INTERNAL_SERVER_ERROR",
                    Message = "An unexpected error occurred.",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.StatusCode;

            await context.Response.WriteAsync(JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }

        private static string ExtractDuplicateField(string message)
        {
            // Extracts the field name from:
            // "Cannot insert duplicate key row in object 'dbo.Clients' with unique index 'IX_Clients_Email'"
            if (message.Contains("IX_Clients_Email"))
                return "A client with this email already exists.";

            if (message.Contains("IX_Clients_Name"))
                return "A client with this name already exists.";

            return "A record with this value already exists.";
        }
    }
}