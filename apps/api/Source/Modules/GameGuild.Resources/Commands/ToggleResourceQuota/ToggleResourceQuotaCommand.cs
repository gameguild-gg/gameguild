using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Command to activate or deactivate a resource quota
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="Type">Type of resource quota to toggle</param>
/// <param name="IsActive">True to activate, false to deactivate</param>
public record ToggleResourceQuotaCommand(Guid TenantId, ResourceUsageType Type, bool IsActive) : ICommand;
