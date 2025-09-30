namespace GameGuild.Modules.Permissions;

/// <summary> Permission constraint for conditional permissions </summary>
public class PermissionConstraint
{
    public string Type { get; set; } = string.Empty; // "resource_owner", "same_tenant", "time_based", etc.

    public string Value { get; set; } = string.Empty; // Constraint value

    public DateTime? ExpiresAt { get; set; } // Optional expiration
}
