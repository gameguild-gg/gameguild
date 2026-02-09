using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to reset a resource quota back to zero usage
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="Type">Type of resource quota to reset</param>
public sealed record ResetResourceQuotaCommand(Guid TenantId, ResourceUsageType Type) : ICommand;
