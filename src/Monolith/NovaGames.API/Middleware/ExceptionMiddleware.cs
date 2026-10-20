using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;
using System.Net;
using System.Text.Json;

namespace NovaGames.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly NovaGames.Core.Interfaces.ILogger _logger;

        public ExceptionMiddleware(RequestDelegate next, NovaGames.Core.Interfaces.ILogger logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Передаємо запит далі по конвеєру
            }
            catch (Exception ex)
            {
                // ЛОГУЄМО КРИТИЧНУ ПОМИЛКУ АПІ
                await _logger.LogAsync($"GLOBAL ERROR: {ex.Message} | StackTrace: {ex.StackTrace}", LogType.ERROR);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "На сервері виникла помилка. Ми вже працюємо над цим."
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}