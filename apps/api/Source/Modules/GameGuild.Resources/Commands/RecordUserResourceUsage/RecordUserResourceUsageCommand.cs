using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to record resource usage for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="ResourceUsageType">Type of usage to record</param>
/// <param name="Count">Usage count</param>
/// <param name="PeriodStart">Usage period start</param>
/// <param name="PeriodEnd">Usage period end</param>
/// <param name="Metadata">Optional metadata</param>
public sealed record RecordUserResourceUsageCommand(Guid UserId, ResourceUsageType ResourceUsageType, long Count, DateTime PeriodStart, DateTime PeriodEnd, string? Metadata = null) : ICommand<Guid>;
