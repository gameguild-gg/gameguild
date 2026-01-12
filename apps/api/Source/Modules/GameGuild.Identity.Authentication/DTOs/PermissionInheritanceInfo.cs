using GameGuild.Identity.Authorization;
﻿namespace GameGuild.Identity.Authentication;

public abstract class PermissionInheritanceInfo
{
    public string Level { get; set; } = string.Empty; // "Tenant", "ContentType", "Resource"

    public List<PermissionType> Permissions { get; set; } = new List<PermissionType>();

    public string Source { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}
