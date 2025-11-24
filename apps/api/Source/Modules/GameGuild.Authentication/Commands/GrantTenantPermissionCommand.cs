using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Enums;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

// Tenant Permission Commands
public record GrantTenantPermissionCommand : ICommand<TenantPermission>
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
