using GameGuild.Identity.Authorization;
﻿using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record HasContentTypePermissionQuery : IQuery<bool>
{
    public Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public string ContentType { get; init; } = string.Empty;

    public PermissionType Permission { get; init; }
}
