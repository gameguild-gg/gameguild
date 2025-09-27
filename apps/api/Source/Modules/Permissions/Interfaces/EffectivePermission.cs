namespace GameGuild.Core.Domain.Permissions;

/// <summary> Effective permission with all metadata </summary>
public class EffectivePermission {
    public PermissionType Permission { get; set; }

    public bool IsGranted { get; set; }

    public PermissionSource Source { get; set; }

    public string SourceDescription { get; set; } = string.Empty;

    public string? GrantedBy { get; set; }

    public DateTime? GrantedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsInherited { get; set; }

    public bool IsExplicit { get; set; }

    public int Priority { get; set; }
}