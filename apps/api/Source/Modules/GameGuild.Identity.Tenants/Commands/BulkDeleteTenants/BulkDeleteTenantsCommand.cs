using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to perform bulk deletion of tenants
/// </summary>
public record BulkDeleteTenantsCommand(IEnumerable<Guid> TenantIds, bool HardDelete = false) : ICommand<BulkOperationResponse>;
