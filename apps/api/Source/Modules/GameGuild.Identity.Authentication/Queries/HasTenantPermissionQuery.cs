using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

// Tenant Permission Queries
public record HasTenantPermissionQuery : IQuery<bool>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public PermissionType Permission { get; init; }
}

// Content Type Permission Queries

// Resource Permission Queries

// Unified Permission Queries

// Permission Analytics Queries

// Permission Template Queries

// ABAC Policy Queries

// Conditional Policy Queries
