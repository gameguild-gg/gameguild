using System.Diagnostics;
using System.Text;
using GameGuild.Core.Logging;
using Serilog.Context;

namespace GameGuild.Core.Middleware;

/// <summary>
/// Enhanced request logging middleware that provides comprehensive request/response logging
/// with correlation IDs, timing information, and user context.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<RequestLoggingMiddleware> _logger;

    private readonly RequestLoggingOptions _options;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, RequestLoggingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);

            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        // Capture request details
        var requestInfo = await CaptureRequestInfoAsync(context.Request);

        // Log request start
        using var requestContext = LoggingExtensions.WithRequestContext(context.Request.Method, context.Request.Path, GetUserAgent(context.Request));

        _logger.LogInformation("Request started: {Method} {Path}{QueryString}", context.Request.Method, context.Request.Path, context.Request.QueryString);

        // Capture response details
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        Exception? exception = null;

        try { await _next(context); }
        catch (Exception ex)
        {
            exception = ex;

            throw;
        }
        finally
        {
            stopwatch.Stop();
            await LogRequestCompletedAsync(context, requestInfo, stopwatch.Elapsed, exception);

            // Copy response body back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<RequestInfo> CaptureRequestInfoAsync(HttpRequest request)
    {
        var requestInfo = new RequestInfo
        {
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            Headers = _options.LogRequestHeaders ? CaptureHeaders(request.Headers) : null
        };

        // Capture request body if enabled and appropriate
        if (_options.LogRequestBody && ShouldLogRequestBody(request)) { requestInfo.Body = await ReadRequestBodyAsync(request); }

        return requestInfo;
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == 0 || request.Body == null) return null;

        try
        {
            request.EnableBuffering();
            request.Body.Position = 0;

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen : true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            // Truncate large bodies
            return body.Length > _options.MaxBodyLength ? $"{body[.._options.MaxBodyLength]}... (truncated)" : body;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read request body");

            return "[Failed to read body]";
        }
    }

    private Task LogRequestCompletedAsync(HttpContext context, RequestInfo requestInfo, TimeSpan elapsed, Exception? exception)
    {
        var response = context.Response;
        var level = DetermineLogLevel(response.StatusCode, elapsed, exception);

        var responseInfo = new
        {
            StatusCode = response.StatusCode, ContentType = response.ContentType, ContentLength = response.ContentLength, Headers = _options.LogResponseHeaders ? CaptureHeaders(response.Headers) : null
        };

        if (exception != null)
        {
            _logger.Log(level, exception, "Request failed: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms", requestInfo.Method, requestInfo.Path, response.StatusCode, elapsed.TotalMilliseconds);
        }
        else { _logger.Log(level, "Request completed: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms", requestInfo.Method, requestInfo.Path, response.StatusCode, elapsed.TotalMilliseconds); }

        // Log performance warning for slow requests
        if (elapsed.TotalMilliseconds > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning("Slow request detected: {Method} {Path} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)", requestInfo.Method, requestInfo.Path, elapsed.TotalMilliseconds, _options.SlowRequestThresholdMs);
        }

        // Add structured logging properties for analytics
        using var _ = LogContext.PushProperty(
            "RequestMetrics",
            new
            {
                requestInfo.Method,
                requestInfo.Path,
                StatusCode = response.StatusCode,
                ElapsedMs = elapsed.TotalMilliseconds,
                requestInfo.ContentLength,
                ResponseContentLength = response.ContentLength,
                IsError = exception != null,
                IsSlowRequest = elapsed.TotalMilliseconds > _options.SlowRequestThresholdMs
            }
        );
        
        return Task.CompletedTask;
    }

    private static LogLevel DetermineLogLevel(int statusCode, TimeSpan elapsed, Exception? exception)
    {
        if (exception != null) return LogLevel.Error;

        if (statusCode >= 500) return LogLevel.Error;

        if (statusCode >= 400) return LogLevel.Warning;

        if (elapsed.TotalMilliseconds > 5000) // 5 seconds
            return LogLevel.Warning;

        return LogLevel.Information;
    }

    private bool ShouldSkipLogging(string path) { return _options.SkipPaths.Any(skipPath => path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase)); }

    private static bool ShouldLogRequestBody(HttpRequest request)
    {
        if (request.ContentLength == 0) return false;

        var contentType = request.ContentType?.ToLowerInvariant();

        if (contentType == null) return false;

        // Only log text-based content types
        return contentType.Contains("application/json") || contentType.Contains("application/xml") || contentType.Contains("text/") || contentType.Contains("application/x-www-form-urlencoded");
    }

    private static Dictionary<string, string> CaptureHeaders(IHeaderDictionary headers)
    {
        var sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Authorization", "Cookie", "Set-Cookie", "X-API-Key", "X-Auth-Token" };

        return headers.Where(h => !sensitiveHeaders.Contains(h.Key)).ToDictionary(h => h.Key, h => string.Join(", ", (string[ ]) h.Value!), StringComparer.OrdinalIgnoreCase);
    }

    private static string GetUserAgent(HttpRequest request) { return request.Headers.UserAgent.FirstOrDefault() ?? "Unknown"; }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Try to get the real client IP from various headers (for reverse proxies)
        var headers = new[ ] { "X-Forwarded-For", "X-Real-IP", "CF-Connecting-IP" };

        foreach (var header in headers)
        {
            if (context.Request.Headers.TryGetValue(header, out var values))
            {
                var ip = values.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();

                if (!string.IsNullOrEmpty(ip)) return ip;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
