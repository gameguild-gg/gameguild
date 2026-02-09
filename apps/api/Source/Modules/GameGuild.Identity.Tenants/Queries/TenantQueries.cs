using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

// Tenant Audit Log Queries
public sealed record GetTenantAuditLogQuery(
    Guid TenantId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Action,
    Guid? ActorId,
    int Page,
    int PageSize
) : IRequest<PagedResult<TenantAuditLogEntry>>;

// Tenant Metadata Queries - Using queries namespace for read operations
public sealed record GetTenantMetadataQuery(Guid TenantId) : IRequest<TenantMetadataDto?>;

public sealed record GetTenantCustomFieldsQuery(Guid TenantId) : IRequest<Dictionary<string, object?>?>;

public sealed record GetTenantTagsQuery(Guid TenantId) : IRequest<List<string>?>;

// Tenant Settings Queries - Using queries namespace for read operations  
public sealed record GetTenantSettingsQuery(Guid TenantId) : IRequest<TenantSettingsDto?>;

public sealed record GetTenantFeatureFlagsQuery(Guid TenantId) : IRequest<Dictionary<string, bool>?>;

public sealed record GetTenantSystemLimitsQuery(Guid TenantId) : IRequest<TenantSystemLimitsDto?>;

public sealed record GetTenantIntegrationSettingsQuery(Guid TenantId) : IRequest<TenantIntegrationSettingsDto?>;
