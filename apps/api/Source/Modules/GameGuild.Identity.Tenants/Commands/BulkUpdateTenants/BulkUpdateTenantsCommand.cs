using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to update multiple tenants at once
/// </summary>
/// <param name="Updates">Collection of tenant updates</param>
public sealed record BulkUpdateTenantsCommand(IEnumerable<BulkUpdateTenantItem> Updates) : ICommand<BulkOperationResponse>;

/// <summary>
///     Data for a single tenant update in a bulk operation
/// </summary>
/// <param name="TenantId">Tenant ID to update</param>
/// <param name="Name">New tenant name</param>
/// <param name="Description">New tenant description</param>
public record BulkUpdateTenantItem(Guid TenantId, string Name, string? Description = null);
