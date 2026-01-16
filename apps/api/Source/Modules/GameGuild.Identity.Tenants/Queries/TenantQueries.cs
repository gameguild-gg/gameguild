using GameGuild.CQRS;
using GameGuild.Models;

namespace GameGuild.Identity.Tenants;

// Tenant Audit Log Queries
public record GetTenantAuditLogQuery(
    Guid TenantId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Action,
    Guid? ActorId,
    int Page,
    int PageSize
) : IRequest<GameGuild.Models.PagedResult<TenantAuditLogEntry>>;

// Tenant Metadata Queries - Using queries namespace for read operations
public record GetTenantMetadataQuery(Guid TenantId) : IRequest<TenantMetadataDto?>;

public record GetTenantCustomFieldsQuery(Guid TenantId) : IRequest<Dictionary<string, object?>?>;

public record GetTenantTagsQuery(Guid TenantId) : IRequest<List<string>?>;

// Tenant Settings Queries - Using queries namespace for read operations  
public record GetTenantSettingsQuery(Guid TenantId) : IRequest<TenantSettingsDto?>;

public record GetTenantFeatureFlagsQuery(Guid TenantId) : IRequest<Dictionary<string, bool>?>;

public record GetTenantSystemLimitsQuery(Guid TenantId) : IRequest<TenantSystemLimitsDto?>;

public record GetTenantIntegrationSettingsQuery(Guid TenantId) : IRequest<TenantIntegrationSettingsDto?>;
