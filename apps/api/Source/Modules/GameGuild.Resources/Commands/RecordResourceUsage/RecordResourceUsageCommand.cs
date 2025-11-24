using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Command to record resource usage
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceUsageType">Type of usage to record</param>
/// <param name="Count">Usage count</param>
/// <param name="PeriodStart">Usage period start</param>
/// <param name="PeriodEnd">Usage period end</param>
/// <param name="Metadata">Optional metadata</param>
public record RecordResourceUsageCommand(Guid TenantId, ResourceUsageType ResourceUsageType, long Count, DateTime PeriodStart, DateTime PeriodEnd, string? Metadata = null) : ICommand<Guid>;
