namespace GameGuild.Resources;

/// <summary>
///     Interface for audit services
/// </summary>
public interface IAuditService
{
    /// <summary>
    ///     Log an audit event
    /// </summary>
    Task LogEventAsync(string eventType, object data, Guid? tenantId = null);
}
