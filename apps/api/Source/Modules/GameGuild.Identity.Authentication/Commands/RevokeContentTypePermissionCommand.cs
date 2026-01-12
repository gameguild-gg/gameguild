using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record RevokeContentTypePermissionCommand : ICommand
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public string ContentType { get; init; } = string.Empty;

    public List<PermissionType> Permissions { get; init; } = new List<PermissionType>();

    public string? RevokedBy { get; init; }

    public string? Reason { get; init; }
}
