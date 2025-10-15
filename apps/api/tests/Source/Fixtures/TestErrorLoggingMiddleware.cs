using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace GameGuild.Tests.Fixtures;

/// <summary>
/// Middleware to capture and log detailed error information during tests
/// </summary>
public class TestErrorLoggingMiddleware(RequestDelegate next, ILogger<TestErrorLoggingMiddleware> logger) {
  public async Task InvokeAsync(HttpContext context) {
    try {
      await next(context);

      // Log details for non-success responses
      if (context.Response.StatusCode >= 400) {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        var hasAuth = !string.IsNullOrEmpty(authHeader);
        var userClaims = context.User.Identity?.IsAuthenticated == true
            ? string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}"))
            : "Not authenticated";

        logger.LogWarning(
            "Request failed with status {StatusCode} for {Method} {Path}. Query: {Query}. HasAuth: {HasAuth}. User: {UserClaims}",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            hasAuth,
            userClaims
        );
      }
    }
    catch (Exception ex) {
      logger.LogError(
          ex,
          "Unhandled exception in request {Method} {Path}. Query: {Query}",
          context.Request.Method,
          context.Request.Path,
          context.Request.QueryString
      );

      throw;
    }
  }
}
