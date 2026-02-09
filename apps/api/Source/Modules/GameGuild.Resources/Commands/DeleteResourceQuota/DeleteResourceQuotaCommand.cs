using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to delete a resource quota
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="Type">Type of resource quota to delete</param>
public sealed record DeleteResourceQuotaCommand(Guid TenantId, ResourceUsageType Type) : ICommand;
