using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to perform bulk deactivation of tenants
/// </summary>
public record BulkDeactivateTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
