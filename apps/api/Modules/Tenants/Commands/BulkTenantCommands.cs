using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to create multiple tenants in bulk
/// </summary>
public sealed record BulkCreateTenantsCommand(IEnumerable<CreateTenantDto> Tenants) : IRequest<IEnumerable<Tenant>>;

/// <summary>
/// DTO for creating a tenant in bulk operations
/// </summary>
public record CreateTenantDto(
    string Name,
    string Slug,
    string? Description = null,
    string? AdminEmail = null,
    bool IsDefault = false
);

/// <summary>
/// Command to update multiple tenants in bulk
/// </summary>
public sealed record BulkUpdateTenantsCommand(IEnumerable<UpdateTenantDto> Tenants) : IRequest<IEnumerable<Tenant>>;

/// <summary>
/// DTO for updating a tenant in bulk operations
/// </summary>
public record UpdateTenantDto(
    Guid Id,
    string? Name = null,
    string? Slug = null,
    string? Description = null,
    string? AdminEmail = null
);

/// <summary>
/// Command to delete multiple tenants in bulk
/// </summary>
public sealed record BulkDeleteTenantsCommand(IEnumerable<Guid> TenantIds, bool SoftDelete = true) : IRequest<int>;

/// <summary>
/// Command to activate multiple tenants in bulk
/// </summary>
public sealed record BulkActivateTenantsCommand(IEnumerable<Guid> TenantIds) : IRequest<int>;

/// <summary>
/// Command to deactivate multiple tenants in bulk
/// </summary>
public sealed record BulkDeactivateTenantsCommand(IEnumerable<Guid> TenantIds) : IRequest<int>;

/// <summary>
/// Command to restore multiple tenants in bulk
/// </summary>
public sealed record BulkRestoreTenantsCommand(IEnumerable<Guid> TenantIds) : IRequest<int>;
