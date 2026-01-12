using GameGuild.CQRS;
using GameGuild.Models;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to perform bulk deactivation of tenants
/// </summary>
public abstract record BulkDeactivateTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
