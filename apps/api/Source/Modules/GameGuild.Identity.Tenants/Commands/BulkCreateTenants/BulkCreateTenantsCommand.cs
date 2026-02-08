using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Command to create multiple tenants at once
/// </summary>
/// <param name="Tenants">Collection of tenant data to create</param>
public record BulkCreateTenantsCommand(IEnumerable<BulkCreateTenantItem> Tenants) : ICommand<BulkOperationResponse>;

/// <summary>
///     Data for a single tenant to create in a bulk operation
/// </summary>
/// <param name="Name">Tenant name</param>
/// <param name="Slug">Tenant slug (unique identifier)</param>
/// <param name="AdminEmail">Administrator email address</param>
/// <param name="Description">Optional tenant description</param>
public record BulkCreateTenantItem(string Name, string Slug, string AdminEmail, string? Description = null);
