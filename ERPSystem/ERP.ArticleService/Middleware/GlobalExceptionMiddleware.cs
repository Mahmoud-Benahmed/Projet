using ERP.ArticleService.Application.Exceptions;

namespace ERP.ArticleService.Middleware
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
                // ── Article
                ArticleNotFoundException => (404, "ART_001", exception.Message),
                ArticleAlreadyExistsException => (400, "ART_002", exception.Message),
                ArticleAlreadyActiveException => (400, "ART_003", exception.Message),
                ArticleAlreadyInactiveException => (400, "ART_004", exception.Message),

                // ── Category
                CategoryNotFoundException => (404, "CAT_001", exception.Message),
                CategoryAlreadyExistsException => (400, "CAT_002", exception.Message),

                // ── Generic
                KeyNotFoundException => (404, "NOT_FOUND", exception.Message),
                ArgumentOutOfRangeException => (400, "OUT_OF_RANGE", exception.Message),
                ArgumentNullException => (400, "NULL_ARGUMENT", exception.Message),
                ArgumentException => (400, "BAD_ARGUMENT", exception.Message),
                InvalidOperationException => (400, "INVALID_OP", exception.Message),
                UnauthorizedAccessException => (401, "UNAUTHORIZED", exception.Message),
                FluentValidation.ValidationException vex => (400, "VALIDATION_ERROR",
                    string.Join(" | ", vex.Errors.Select(e => e.ErrorMessage))),

                _ => (500, "SERVER_ERROR", exception.Message)
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsJsonAsync(new { statusCode, code, message });
        }
    }
}