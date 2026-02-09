using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to activate or deactivate a resource quota
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="Type">Type of resource quota to toggle</param>
/// <param name="IsActive">True to activate, false to deactivate</param>
public sealed record ToggleResourceQuotaCommand(Guid TenantId, ResourceUsageType Type, bool IsActive) : ICommand;
