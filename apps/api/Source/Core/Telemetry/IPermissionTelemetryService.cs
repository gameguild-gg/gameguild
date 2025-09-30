namespace GameGuild.Core.Telemetry;

/// <summary>
/// Service for adding telemetry to permission checks
/// </summary>
public interface IPermissionTelemetryService
{
    /// <summary>
    /// Records a permission check operation with telemetry
    /// </summary>
    Task<bool> RecordPermissionCheckAsync(string permission, string? resourceType, Guid? resourceId, Func<Task<bool>> permissionCheck);
}
