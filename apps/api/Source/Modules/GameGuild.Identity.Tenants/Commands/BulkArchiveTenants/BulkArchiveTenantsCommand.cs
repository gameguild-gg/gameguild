using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to perform bulk archival of tenants
/// </summary>
public record BulkArchiveTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
