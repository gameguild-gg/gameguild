using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

// Tenant Metadata Queries - Using queries namespace for read operations
public record GetTenantMetadataQuery(Guid TenantId) : IRequest<TenantMetadataDto?>;

public record GetTenantCustomFieldsQuery(Guid TenantId) : IRequest<Dictionary<string, object?>?>;

public record GetTenantTagsQuery(Guid TenantId) : IRequest<List<string>?>;

// Tenant Settings Queries - Using queries namespace for read operations  
public record GetTenantSettingsQuery(Guid TenantId) : IRequest<TenantSettingsDto?>;

public record GetTenantFeatureFlagsQuery(Guid TenantId) : IRequest<Dictionary<string, bool>?>;

public record GetTenantSystemLimitsQuery(Guid TenantId) : IRequest<TenantSystemLimitsDto?>;

public record GetTenantIntegrationSettingsQuery(Guid TenantId) : IRequest<TenantIntegrationSettingsDto?>;
