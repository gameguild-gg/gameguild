using GameGuild.Identity.Authorization;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

// Tenant Permission Commands
public sealed record GrantTenantPermissionCommand : ICommand<TenantPermission>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public List<PermissionType> Permissions { get; init; } = new List<PermissionType>();

    public DateTime? ExpiresAt { get; init; }

    public string? GrantedBy { get; init; }

    public string? Reason { get; init; }
}

// Content Type Permission Commands

// Resource Permission Commands

// Cache Management Commands

// Permission Template Commands
