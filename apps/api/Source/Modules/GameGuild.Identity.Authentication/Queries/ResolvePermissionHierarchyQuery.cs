using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record ResolvePermissionHierarchyQuery : IQuery<PermissionHierarchyResult>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public PermissionType Permission { get; init; }

    public Guid? ResourceId { get; init; }

    public string? ContentType { get; init; }
}
