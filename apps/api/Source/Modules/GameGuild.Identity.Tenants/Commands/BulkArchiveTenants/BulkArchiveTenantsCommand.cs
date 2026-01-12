using GameGuild.CQRS;
using GameGuild.Models;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to perform bulk archival of tenants
/// </summary>
public abstract record BulkArchiveTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
