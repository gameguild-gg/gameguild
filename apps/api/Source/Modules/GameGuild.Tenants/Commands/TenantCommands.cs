using GameGuild.CQRS;
using GameGuild.Tenants.Models;

namespace GameGuild.Tenants.Commands;

// Tenant Metadata Commands
public record UpdateTenantMetadataCommand(Guid TenantId, UpdateTenantMetadataRequest Request) : IRequest;

public record ReplaceTenantMetadataCommand(Guid TenantId, ReplaceTenantMetadataRequest Request) : IRequest;

public record UpdateTenantCustomFieldsCommand(Guid TenantId, UpdateTenantCustomFieldsRequest Request) : IRequest;

public record UpdateTenantTagsCommand(Guid TenantId, UpdateTenantTagsRequest Request) : IRequest;

public record ReplaceTenantTagsCommand(Guid TenantId, ReplaceTenantTagsRequest Request) : IRequest;

// Tenant Settings Commands
public record UpdateTenantSettingsCommand(Guid TenantId, UpdateTenantSettingsRequest Request) : IRequest;

public record ReplaceTenantSettingsCommand(Guid TenantId, ReplaceTenantSettingsRequest Request) : IRequest;

public record UpdateTenantFeatureFlagsCommand(Guid TenantId, UpdateTenantFeatureFlagsRequest Request) : IRequest;

public record UpdateTenantSystemLimitsCommand(Guid TenantId, UpdateTenantSystemLimitsRequest Request) : IRequest;

public record UpdateTenantIntegrationSettingsCommand(Guid TenantId, UpdateTenantIntegrationSettingsRequest Request) : IRequest;
