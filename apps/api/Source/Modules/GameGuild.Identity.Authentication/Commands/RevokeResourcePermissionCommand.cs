using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record RevokeResourcePermissionCommand : ICommand<bool>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public Guid ResourceId { get; init; }

    public List<PermissionType> Permissions { get; init; } = new List<PermissionType>();

    public string? RevokedBy { get; init; }

    public string? Reason { get; init; }
}
