using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to permanently purge multiple tenants (irreversible hard delete)
/// </summary>
/// <param name="TenantIds">Collection of tenant IDs to permanently delete</param>
public record BulkPurgeTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
