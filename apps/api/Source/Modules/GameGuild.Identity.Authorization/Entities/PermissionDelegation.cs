using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Permission delegation allows users to delegate their permissions to other users
/// </summary>
public class PermissionDelegation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DelegatorUserId { get; set; }

    public Guid DelegateUserId { get; set; }

    public TenantId? TenantId { get; set; }

    public Guid? ResourceId { get; set; }

    public string[] DelegatedPermissions { get; set; } = Array.Empty<string>();

    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool CanSubDelegate { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public string? Reason { get; set; }

    public string? Conditions { get; set; } // JSON serialized conditions

    public int? UsageLimit { get; set; }

    public int UsageCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Check if delegation is currently valid and active
    /// </summary>
    public bool IsValidNow() =>
        IsActive &&
        StartsAt <= DateTime.UtcNow &&
        (ExpiresAt == null || ExpiresAt > DateTime.UtcNow) &&
        (UsageLimit == null || UsageCount < UsageLimit);

    /// <summary>
    ///     Check if delegation allows a specific permission
    /// </summary>
    public bool AllowsPermission(string permission) =>
        IsValidNow() && DelegatedPermissions.Contains(permission);

    /// <summary>
    ///     Record usage of the delegation
    /// </summary>
    public void RecordUsage()
    {
        if (!IsValidNow())
            throw new InvalidOperationException("Cannot record usage for invalid delegation");

        UsageCount++;
        UpdatedAt = DateTime.UtcNow;

        // Auto-deactivate if usage limit reached
        if (UsageLimit.HasValue && UsageCount >= UsageLimit.Value)
        {
            IsActive = false;
        }
    }

    /// <summary>
    ///     Activate the delegation
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Deactivate/revoke the delegation
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if delegation has expired
    /// </summary>
    public bool IsExpired() => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    /// <summary>
    ///     Extend the expiration date
    /// </summary>
    public void Extend(DateTime newExpiresAt)
    {
        if (newExpiresAt <= DateTime.UtcNow)
            throw new ArgumentException("New expiration must be in the future", nameof(newExpiresAt));

        ExpiresAt = newExpiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if delegation has usage limit remaining
    /// </summary>
    public bool HasUsageRemaining() =>
        !UsageLimit.HasValue || UsageCount < UsageLimit.Value;

    /// <summary>
    ///     Get remaining usage count
    /// </summary>
    public int? GetRemainingUsage()
    {
        if (!UsageLimit.HasValue) return null;
        return Math.Max(0, UsageLimit.Value - UsageCount);
    }
}
