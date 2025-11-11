using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Command to reset a resource quota back to zero usage
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="Type">Type of resource quota to reset</param>
public record ResetResourceQuotaCommand(Guid TenantId, ResourceUsageType Type) : ICommand;
