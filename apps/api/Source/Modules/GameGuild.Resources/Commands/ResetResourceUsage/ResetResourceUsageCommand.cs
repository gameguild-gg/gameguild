using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Command to reset resource usage for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceUsageType">Optional usage type filter</param>
public record ResetResourceUsageCommand(Guid TenantId, ResourceUsageType? ResourceUsageType = null) : ICommand;
