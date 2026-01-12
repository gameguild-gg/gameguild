using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record BulkGrantResourcePermissionsCommand : ICommand<BulkPermissionResult>
{
    public List<Guid> UserIds { get; init; } = new List<Guid>();

    public Guid TenantId { get; init; }

    public Guid ResourceId { get; init; }

    public string ResourceType { get; init; } = string.Empty;

    public List<PermissionType> Permissions { get; init; } = new List<PermissionType>();

    public DateTime? ExpiresAt { get; init; }

    public string? GrantedBy { get; init; }

    public string? Reason { get; init; }
}
