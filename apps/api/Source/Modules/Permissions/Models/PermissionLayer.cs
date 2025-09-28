namespace GameGuild.Core.Domain.Permissions;

/// <summary> Individual permission layer in the hierarchy </summary>
public class PermissionLayer {
    public PermissionSource Source { get; set; }

    public bool? IsGranted { get; set; }

    public bool IsDefault { get; set; }

    public string? GrantedBy { get; set; }

    public DateTime? GrantedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int Priority { get; set; }

    public string Description { get; set; } = string.Empty;
}