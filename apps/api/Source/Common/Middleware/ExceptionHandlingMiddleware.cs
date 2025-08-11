using System.Net;
using System.Text.Json;


namespace GameGuild.Common;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger) {
  public async Task InvokeAsync(HttpContext context) {
    try { await next(context); }
    catch (Exception ex) {
      logger.LogError(ex, "An unhandled exception occurred");
      await HandleExceptionAsync(context, ex);
    }
  }

  private static async Task HandleExceptionAsync(HttpContext context, Exception exception) {
    var response = context.Response;
    response.ContentType = "application/json";

    var errorResponse = new { message = exception.Message, statusCode = (int)HttpStatusCode.InternalServerError, timestamp = DateTime.UtcNow };

    response.StatusCode = (int)HttpStatusCode.InternalServerError;

    var jsonResponse = JsonSerializer.Serialize(errorResponse);
    await response.WriteAsync(jsonResponse);
  }
}
