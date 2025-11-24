using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Entities;

/// <summary>
///     Entity for Just-in-Time (JIT) permission elevation requests
///     Enables time-bound temporary permission grants with approval workflow
/// </summary>
public class JitElevationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequesterId { get; set; }

    public TenantId? TenantId { get; set; }

    public string Permission { get; set; } = string.Empty; // TODO: Link to PermissionType enum

    public string? ResourceType { get; set; }

    public Guid? ResourceId { get; set; }

    public string Justification { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public ElevationRequestStatus Status { get; set; } = ElevationRequestStatus.Pending;

    public Guid? ReviewerId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewerComments { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? RevokedBy { get; set; }

    public string? RevocationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Check if request is currently active
    /// </summary>
    public bool IsActive()
    {
        if (Status != ElevationRequestStatus.Active) return false;

        var now = DateTime.UtcNow;
        var startTime = StartsAt ?? CreatedAt;

        return now >= startTime && now < ExpiresAt;
    }

    /// <summary>
    ///     Check if request has expired
    /// </summary>
    public bool IsExpired() { return Status == ElevationRequestStatus.Active && DateTime.UtcNow >= ExpiresAt; }

    /// <summary>
    ///     Approve the elevation request
    /// </summary>
    public void Approve(Guid reviewerId, string? comments = null)
    {
        if (Status != ElevationRequestStatus.Pending) throw new InvalidOperationException("Only pending requests can be approved");

        Status = ElevationRequestStatus.Approved;
        ReviewerId = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        ReviewerComments = comments;
        UpdatedAt = DateTime.UtcNow;

        // Auto-activate if no start time specified
        if (StartsAt == null || StartsAt <= DateTime.UtcNow) { Activate(); }
    }

    /// <summary>
    ///     Deny the elevation request
    /// </summary>
    public void Deny(Guid reviewerId, string comments)
    {
        if (Status != ElevationRequestStatus.Pending) throw new InvalidOperationException("Only pending requests can be denied");

        Status = ElevationRequestStatus.Denied;
        ReviewerId = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        ReviewerComments = comments;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Activate the approved elevation
    /// </summary>
    public void Activate()
    {
        if (Status != ElevationRequestStatus.Approved) throw new InvalidOperationException("Only approved requests can be activated");

        Status = ElevationRequestStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Revoke the active elevation
    /// </summary>
    public void Revoke(Guid revokedBy, string reason)
    {
        if (Status != ElevationRequestStatus.Active && Status != ElevationRequestStatus.Approved) throw new InvalidOperationException("Only active or approved requests can be revoked");

        Status = ElevationRequestStatus.Revoked;
        RevokedBy = revokedBy;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Mark as expired
    /// </summary>
    public void MarkExpired()
    {
        if (Status == ElevationRequestStatus.Active)
        {
            Status = ElevationRequestStatus.Expired;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     Calculate remaining time in minutes
    /// </summary>
    public int GetRemainingMinutes()
    {
        if (!IsActive()) return 0;

        var remaining = ExpiresAt - DateTime.UtcNow;

        return (int) Math.Max(0, remaining.TotalMinutes);
    }
}
