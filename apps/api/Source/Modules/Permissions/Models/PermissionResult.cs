namespace GameGuild.Core.Domain.Permissions;

/// <summary> Detailed permission resolution result </summary>
public class PermissionResult {
    public bool IsGranted { get; set; }

    public bool IsExplicitlyDenied { get; set; }

    public PermissionSource Source { get; set; }

    public string? GrantedBy { get; set; }

    public DateTime? GrantedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? Reason { get; set; }

    public int Priority { get; set; }

    public bool IsInherited { get; set; }
}