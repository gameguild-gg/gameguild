using Serilog.Context;

namespace GameGuild.Core.Logging;

/// <summary>
/// Extension methods for adding structured logging context
/// </summary>
public static class LoggingContextExtensions
{
    /// <summary>
    /// Adds correlation ID to the logging context
    /// </summary>
    public static IDisposable WithCorrelationId(string correlationId)
    {
        return LogContext.PushProperty("CorrelationId", correlationId);
    }

    /// <summary>
    /// Adds user context to the logging context
    /// </summary>
    public static IDisposable WithUserContext(Guid userId, string? userName = null, string? email = null)
    {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("UserId", userId)
        };

        if (!string.IsNullOrWhiteSpace(userName))
        {
            disposables.Add(LogContext.PushProperty("UserName", userName));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            disposables.Add(LogContext.PushProperty("UserEmail", email));
        }

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Adds tenant context to the logging context
    /// </summary>
    public static IDisposable WithTenantContext(Guid tenantId, string? tenantName = null)
    {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("TenantId", tenantId)
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            disposables.Add(LogContext.PushProperty("TenantName", tenantName));
        }

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// Adds permission context to the logging context
    /// </summary>
    public static IDisposable WithPermissionContext(string permission, string? resource = null)
    {
        var disposables = new List<IDisposable>
        {
            LogContext.PushProperty("Permission", permission)
        };

        if (!string.IsNullOrWhiteSpace(resource))
        {
            disposables.Add(LogContext.PushProperty("Resource", resource));
        }

        return new CompositeDisposable(disposables);
    }

    private class CompositeDisposable : IDisposable
    {
        private readonly IEnumerable<IDisposable> _disposables;

        public CompositeDisposable(IEnumerable<IDisposable> disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
