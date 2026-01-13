using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to record resource usage
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceUsageType">Type of usage to record</param>
/// <param name="Count">Usage count</param>
/// <param name="PeriodStart">Usage period start</param>
/// <param name="PeriodEnd">Usage period end</param>
/// <param name="Metadata">Optional metadata (JSON string)</param>
/// <param name="Source">Optional source identifier (e.g., "API", "UI", "System")</param>
/// <param name="SkipQuotaIncrement">If true, only creates the usage record without incrementing quota (use when quota was already atomically consumed)</param>
public record RecordResourceUsageCommand(
    Guid TenantId,
    ResourceUsageType ResourceUsageType,
    long Count,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string? Metadata = null,
    string? Source = null,
    bool SkipQuotaIncrement = false) : ICommand<Guid>;
