namespace GameGuild.Core.Domain;
using GameGuild.Modules.Resources;

/// <summary>
/// Result of a permission check operation
/// </summary>
public class PermissionResult
{
    public bool IsGranted { get; init; }

    public string? Reason { get; init; }

    public PermissionSource Source { get; init; } = PermissionSource.NotGranted;

    public static PermissionResult Granted(PermissionSource source, string? reason = null) => new() { IsGranted = true, Source = source, Reason = reason };

    public static PermissionResult Denied(string? reason = null) => new() { IsGranted = false, Reason = reason };
}

/// <summary>
/// Source of permission grant
/// </summary>
public enum PermissionSource { NotGranted, TenantWide, ContentType, Resource, Owner }

/// <summary>
/// Effective permission details
/// </summary>
public class EffectivePermission
{
    public PermissionType Permission { get; init; }

    public PermissionSource Source { get; init; }

    public string? Context { get; init; }

    public DateTime GrantedAt { get; init; }
}

/// <summary>
/// Permission hierarchy for debugging
/// </summary>
public class PermissionHierarchy
{
    public PermissionType Permission { get; init; }

    public Dictionary<PermissionSource, bool> Sources { get; init; } = new();

    public string? FinalDecision { get; init; }
}
