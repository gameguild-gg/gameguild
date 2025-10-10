using GameGuild.Core.Domain;

namespace GameGuild.Modules.Users.Entities;

/// <summary>
/// Represents a user's request to deactivate their account with a grace period before permanent deletion.
/// Implements a soft-lock workflow where the account is temporarily disabled before final removal.
/// </summary>
public class AccountDeactivationRequest : EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the user requesting deactivation.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the reason for deactivation provided by the user.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the detailed feedback from the user about why they are leaving.
    /// </summary>
    public string? Feedback { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the deactivation was requested.
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// Gets or sets the scheduled date and time for permanent deletion.
    /// Typically set to RequestedAt + grace period (e.g., 30 days).
    /// </summary>
    public DateTime? ScheduledDeletionAt { get; set; }

    /// <summary>
    /// Gets or sets the current status of the deactivation request.
    /// </summary>
    public DeactivationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the request was cancelled (if applicable).
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was permanently deleted.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the deactivation was requested.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent of the browser/client used to request deactivation.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the deactivation request (JSON format).
    /// Can include survey responses, exit interview data, etc.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the number of reminder notifications sent to the user.
    /// </summary>
    public int RemindersSent { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the last reminder sent.
    /// </summary>
    public DateTime? LastReminderSentAt { get; set; }

    /// <summary>
    /// Gets whether the deactivation is still pending (not cancelled or completed).
    /// </summary>
    public bool IsPending => Status == DeactivationStatus.Pending;

    /// <summary>
    /// Gets whether the grace period has expired and deletion is due.
    /// </summary>
    public bool IsDue => ScheduledDeletionAt.HasValue && ScheduledDeletionAt.Value <= DateTime.UtcNow;

    /// <summary>
    /// Gets the number of days remaining in the grace period.
    /// </summary>
    public int? DaysRemaining
    {
        get
        {
            if (!ScheduledDeletionAt.HasValue) return null;
            var days = (ScheduledDeletionAt.Value - DateTime.UtcNow).TotalDays;
            return days > 0 ? (int)Math.Ceiling(days) : 0;
        }
    }

    /// <summary>
    /// Cancels the deactivation request and reactivates the account.
    /// </summary>
    public void Cancel()
    {
        Status = DeactivationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the deactivation as completed after permanent deletion.
    /// </summary>
    public void MarkCompleted()
    {
        Status = DeactivationStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records that a reminder was sent to the user.
    /// </summary>
    public void RecordReminderSent()
    {
        RemindersSent++;
        LastReminderSentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the status of an account deactivation request.
/// </summary>
public enum DeactivationStatus
{
    /// <summary>
    /// The deactivation request is pending, awaiting the grace period to expire.
    /// Account is soft-locked (user cannot login but data is preserved).
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The deactivation request was cancelled by the user.
    /// Account is reactivated and restored to normal state.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// The deactivation was completed and the account has been permanently deleted.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The deactivation request failed during processing.
    /// </summary>
    Failed = 3
}
