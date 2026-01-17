using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record GetContentTypePermissionsQuery : IQuery<IEnumerable<PermissionType>>
{
    public Guid UserId { get; init; }

    public Guid? TenantId { get; init; }

    public string? ContentType { get; init; }
}
