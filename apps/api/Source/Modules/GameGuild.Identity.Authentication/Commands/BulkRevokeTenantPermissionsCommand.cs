using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record BulkRevokeTenantPermissionsCommand : ICommand<BulkPermissionResult>
{
    public List<Guid> UserIds { get; init; } = new List<Guid>();

    public Guid TenantId { get; init; }

    public List<PermissionType> Permissions { get; init; } = new List<PermissionType>();

    public string? RevokedBy { get; init; }

    public string? Reason { get; init; }
}
