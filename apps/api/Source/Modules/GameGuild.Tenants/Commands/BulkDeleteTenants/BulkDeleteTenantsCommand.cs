using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to perform bulk deletion of tenants
/// </summary>
public abstract record BulkDeleteTenantsCommand(IEnumerable<Guid> TenantIds, bool HardDelete = false) : ICommand<BulkOperationResponse>;
