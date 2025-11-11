using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Command to delete a resource quota
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="Type">Type of resource quota to delete</param>
public record DeleteResourceQuotaCommand(Guid TenantId, ResourceUsageType Type) : ICommand;
