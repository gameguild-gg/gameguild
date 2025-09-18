using Serilog.Context;


namespace GameGuild.Authorization.Middleware;

/// <summary> Middleware for adding request context to logs Adds correlation ID for request tracing across services </summary>
public class RequestContextLoggingMiddleware {
  private const string CorrelationIdHeaderName = "Correlation-Id";

  private readonly RequestDelegate _next;

  public RequestContextLoggingMiddleware(RequestDelegate next) { _next = next; }

  public Task Invoke(HttpContext context) {
    using (LogContext.PushProperty("CorrelationId", GetCorrelationId(context))) { return _next.Invoke(context); }
  }

  private static string GetCorrelationId(HttpContext context) {
    context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId);

    return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
  }
}
