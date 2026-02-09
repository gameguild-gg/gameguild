using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record GetTenantPermissionsQuery : IQuery<IEnumerable<PermissionType>>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }
}
