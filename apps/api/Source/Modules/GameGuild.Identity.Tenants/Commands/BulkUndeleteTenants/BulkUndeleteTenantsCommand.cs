using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to restore multiple soft-deleted tenants at once
/// </summary>
/// <param name="TenantIds">Collection of tenant IDs to restore</param>
public sealed record BulkUndeleteTenantsCommand(IEnumerable<Guid> TenantIds) : ICommand<BulkOperationResponse>;
