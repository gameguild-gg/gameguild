using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

// Tenant Validation Commands
public sealed record ValidateTenantCommand(string Name, string Slug, string AdminEmail) : IRequest<TenantValidationResponse>;

// Tenant Metadata Commands
public sealed record UpdateTenantMetadataCommand(Guid TenantId, UpdateTenantMetadataRequest Request) : IRequest;

public sealed record ReplaceTenantMetadataCommand(Guid TenantId, ReplaceTenantMetadataRequest Request) : IRequest;

public sealed record UpdateTenantCustomFieldsCommand(Guid TenantId, UpdateTenantCustomFieldsRequest Request) : IRequest;

public sealed record UpdateTenantTagsCommand(Guid TenantId, UpdateTenantTagsRequest Request) : IRequest;

public sealed record ReplaceTenantTagsCommand(Guid TenantId, ReplaceTenantTagsRequest Request) : IRequest;

// Tenant Settings Commands
public sealed record UpdateTenantSettingsCommand(Guid TenantId, UpdateTenantSettingsRequest Request) : IRequest;

public sealed record ReplaceTenantSettingsCommand(Guid TenantId, ReplaceTenantSettingsRequest Request) : IRequest;

public sealed record UpdateTenantFeatureFlagsCommand(Guid TenantId, UpdateTenantFeatureFlagsRequest Request) : IRequest;

public sealed record UpdateTenantSystemLimitsCommand(Guid TenantId, UpdateTenantSystemLimitsRequest Request) : IRequest;

public sealed record UpdateTenantIntegrationSettingsCommand(Guid TenantId, UpdateTenantIntegrationSettingsRequest Request) : IRequest;
