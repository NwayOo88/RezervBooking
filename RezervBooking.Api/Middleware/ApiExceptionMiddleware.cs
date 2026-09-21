using RezervBooking.Application.Common;
using System.Text.Json;

namespace RezervBooking.Api.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware>
            _logger;

        public ApiExceptionMiddleware(
            RequestDelegate next,
            ILogger<ApiExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessRuleException ex)
            {
                context.Response.StatusCode =
                    ex.StatusCode;

                context.Response.ContentType =
                    "application/json";

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(
                        new
                        {
                            code = ex.Code,
                            message = ex.Message
                        }));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception");

                context.Response.StatusCode = 500;

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        code = "SYSTEM_ERROR",
                        message =
                            "An unexpected error occurred."
                    });
            }
        }
    }
}
