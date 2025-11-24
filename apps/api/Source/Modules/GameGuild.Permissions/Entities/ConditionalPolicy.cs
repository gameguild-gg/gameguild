using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Entities;

/// <summary>
///     Represents a conditional policy that enforces permission rules based on context
/// </summary>
public class ConditionalPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PolicyConditionType ConditionType { get; set; }

    public string? PermissionType { get; set; } // TODO: Link to PermissionType enum from existing system

    public string? ResourceType { get; set; }

    public PolicyAction Action { get; set; }

    public int Priority { get; set; }

    public bool IsEnabled { get; set; } = true;

    // Condition details stored as JSON
    public string? TimeConditions { get; set; }

    public string? EnvironmentConditions { get; set; }

    public string? LocationConditions { get; set; }

    public string? DeviceConditions { get; set; }

    public string? CustomConditions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    /// <summary>
    ///     Check if policy is currently active
    /// </summary>
    public bool IsActive() { return IsEnabled; }

    /// <summary>
    ///     Enable the policy
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Disable the policy
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Update priority
    /// </summary>
    public void SetPriority(int priority)
    {
        if (priority < 0) throw new ArgumentException("Priority must be non-negative", nameof(priority));

        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if policy applies to a specific permission type
    /// </summary>
    public bool AppliesTo(string permissionType) { return PermissionType == null || PermissionType == permissionType; }

    /// <summary>
    ///     Check if policy applies to a specific resource type
    /// </summary>
    public bool AppliesToResourceType(string resourceType) { return ResourceType == null || ResourceType == resourceType; }
}
