using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to create or update a resource quota for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Type">Type of resource to set quota for</param>
/// <param name="SoftLimit">Soft limit threshold (warning level)</param>
/// <param name="HardLimit">Hard limit threshold (enforcement level)</param>
/// <param name="Period">Reset period for the quota</param>
/// <param name="IsActive">Whether the quota is active</param>
/// <param name="ResetTime">Optional time of day for resets</param>
public sealed record SetUserResourceQuotaCommand(
    Guid UserId,
    ResourceUsageType Type,
    int? SoftLimit,
    int? HardLimit,
    ResourceQuotaPeriod Period = ResourceQuotaPeriod.Monthly,
    bool IsActive = true,
    TimeSpan? ResetTime = null
) : ICommand;
