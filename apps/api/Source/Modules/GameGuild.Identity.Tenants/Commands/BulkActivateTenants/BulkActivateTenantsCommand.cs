using GameGuild.CQRS;
using GameGuild.Models;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to perform bulk activation of tenants
/// </summary>
public abstract record BulkActivateTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
