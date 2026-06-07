using System.Net;
using System.Text.Json;

namespace RetradeBE.Middlewares
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var errorMessage = exception.InnerException != null ? exception.InnerException.Message : exception.Message;

            return context.Response.WriteAsync(errorMessage);
        }
    }
}
