using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to reset resource usage for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceUsageType">Optional usage type filter</param>
public sealed record ResetResourceUsageCommand(Guid TenantId, ResourceUsageType? ResourceUsageType = null) : ICommand;
