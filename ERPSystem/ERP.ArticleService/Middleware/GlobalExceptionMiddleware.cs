using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace ERP.ArticleService.Middleware
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
                // ── Article
                ArticleNotFoundException ex => new ErrorResponse
                {
                    Code = "ART_001",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.NotFound
                },

                ArticleAlreadyExistsException ex => new ErrorResponse
                {
                    Code = "ART_002",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                ArticleAlreadyActiveException ex => new ErrorResponse
                {
                    Code = "ART_003",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                ArticleAlreadyInactiveException ex => new ErrorResponse
                {
                    Code = "ART_004",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                // ── Category
                CategoryNotFoundException ex => new ErrorResponse
                {
                    Code = "CAT_001",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.NotFound
                },

                CategoryAlreadyExistsException ex => new ErrorResponse
                {
                    Code = "CAT_002",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                CategoryAssignedToArticlesException ex => new ErrorResponse
                {
                    Code = "ARTICLE_CATEGORY_DELETE_FAIL",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.Conflict
                },
                // ── Database
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

                // ── Generic
                KeyNotFoundException ex => new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.NotFound
                },

                ArgumentOutOfRangeException ex => new ErrorResponse
                {
                    Code = "OUT_OF_RANGE",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                ArgumentNullException ex => new ErrorResponse
                {
                    Code = "NULL_ARGUMENT",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                ArgumentException ex => new ErrorResponse
                {
                    Code = "BAD_ARGUMENT",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                InvalidOperationException ex => new ErrorResponse
                {
                    Code = "INVALID_OP",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                },

                UnauthorizedAccessException ex => new ErrorResponse
                {
                    Code = "UNAUTHORIZED",
                    Message = ex.Message,
                    StatusCode = (int)HttpStatusCode.Unauthorized
                },

                _ => new ErrorResponse
                {
                    Code = "SERVER_ERROR",
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
            if (message.Contains("IX_Articles_CodeRef"))
                return "An article with this code already exists.";

            if (message.Contains("IX_Articles_BarCode"))
                return "An article with this barcode already exists.";

            if (message.Contains("IX_Categories_Name"))
                return "A category with this name already exists.";

            return "A record with this value already exists.";
        }
    }
}