using GameGuild.Identity.Authorization;
﻿namespace GameGuild.Identity.Authentication;

public abstract class PermissionHierarchyResult
{
    public bool HasPermission { get; set; }

    public PermissionType RequestedPermission { get; set; }

    public List<PermissionResolutionStep> ResolutionSteps { get; set; } = new List<PermissionResolutionStep>();

    public string FinalSource { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}
