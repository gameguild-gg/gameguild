using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record HasResourcePermissionQuery : IQuery<bool>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public Guid ResourceId { get; init; }

    public PermissionType Permission { get; init; }
}
