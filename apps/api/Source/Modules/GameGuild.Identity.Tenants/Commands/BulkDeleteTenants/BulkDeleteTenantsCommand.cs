using GameGuild.CQRS;
using GameGuild.Models;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to perform bulk deletion of tenants
/// </summary>
public abstract record BulkDeleteTenantsCommand(IEnumerable<Guid> TenantIds, bool HardDelete = false) : ICommand<BulkOperationResponse>;
