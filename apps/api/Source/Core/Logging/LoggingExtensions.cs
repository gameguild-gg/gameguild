using Serilog.Context;

namespace GameGuild.Core.Logging;

/// <summary>
/// Extension methods for logging operations.
/// </summary>
public static class LoggingExtensions {
    /// <summary>
    /// Creates a disposable context that adds a correlation ID to the log scope.
    /// </summary>
    /// <param name="correlationId">The correlation ID to add to the log context</param>
    /// <returns>A disposable context that will remove the correlation ID when disposed</returns>
    public static IDisposable WithCorrelationId(string correlationId) {
        return LogContext.PushProperty("CorrelationId", correlationId);
    }

    /// <summary>
    /// Adds user context to logs
    /// </summary>
    public static IDisposable WithUserContext(Guid? userId, Guid? tenantId = null) {
        var disposables = new List<IDisposable>();

        if (userId.HasValue)
            disposables.Add(LogContext.PushProperty("UserId", userId.Value));

        if (tenantId.HasValue)
            disposables.Add(LogContext.PushProperty("TenantId", tenantId.Value));

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Adds permission evaluation context to logs
    /// </summary>
    public static IDisposable WithPermissionContext(string permission, string? resourceType = null, Guid? resourceId = null) {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("Permission", permission)
        };

        if (!string.IsNullOrEmpty(resourceType))
            disposables.Add(LogContext.PushProperty("ResourceType", resourceType));

        if (resourceId.HasValue)
            disposables.Add(LogContext.PushProperty("ResourceId", resourceId.Value));

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Adds request context to logs
    /// </summary>
    public static IDisposable WithRequestContext(string method, string path, string? userAgent = null) {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("HttpMethod", method),
            LogContext.PushProperty("RequestPath", path)
        };

        if (!string.IsNullOrEmpty(userAgent))
            disposables.Add(LogContext.PushProperty("UserAgent", userAgent));

        return new CompositeDisposable(disposables);
    }
}

/// <summary>
/// Helper class to dispose multiple disposables
/// </summary>
internal class CompositeDisposable : IDisposable {
    private readonly List<IDisposable> _disposables;
    private bool _disposed;

    public CompositeDisposable(List<IDisposable> disposables) {
        _disposables = disposables ?? throw new ArgumentNullException(nameof(disposables));
    }

    public void Dispose() {
        if (_disposed) return;

        foreach (var disposable in _disposables) {
            try {
                disposable?.Dispose();
            }
            catch {
                // Ignore disposal errors
            }
        }

        _disposed = true;
    }
}
