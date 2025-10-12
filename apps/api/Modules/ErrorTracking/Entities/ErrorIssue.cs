namespace GameGuild.Modules.ErrorTracking.Entities;

/// <summary>
/// Represents an aggregated error issue (group of similar error events).
/// </summary>
public class ErrorIssue : EntityBase {
    /// <summary>
    /// Gets or sets the tenant ID this issue belongs to.
    /// </summary>
    public new Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the unique fingerprint for this issue.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title/summary of the issue.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception type.
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the representative error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the issue.
    /// </summary>
    public IssueStatus Status { get; set; } = IssueStatus.Unresolved;

    /// <summary>
    /// Gets or sets the total count of occurrences.
    /// </summary>
    public int EventCount { get; set; }

    /// <summary>
    /// Gets or sets the count of affected users.
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Gets or sets when this issue was first seen.
    /// </summary>
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this issue was last seen.
    /// </summary>
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the severity level (based on most severe event).
    /// </summary>
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

    /// <summary>
    /// Gets or sets the environments where this issue occurs.
    /// </summary>
    public string? Environments { get; set; }

    /// <summary>
    /// Gets or sets the releases/versions affected.
    /// </summary>
    public string? Releases { get; set; }

    /// <summary>
    /// Gets or sets who the issue is assigned to.
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Gets or sets when the issue was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets who resolved the issue.
    /// </summary>
    public Guid? ResolvedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the resolution notes.
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Gets or sets whether alerts are muted for this issue.
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// Gets or sets when alerts should be unmuted.
    /// </summary>
    public DateTime? MutedUntil { get; set; }

    /// <summary>
    /// Gets or sets the collection of error events for this issue.
    /// </summary>
    public ICollection<ErrorEvent> Events { get; set; } = new List<ErrorEvent>();

    /// <summary>
    /// Records a new error event occurrence.
    /// </summary>
    public void RecordEvent(Guid? userId = null) {
        EventCount++;
        LastSeenAt = DateTime.UtcNow;

        if (userId.HasValue) {
            UserCount++;
        }
    }

    /// <summary>
    /// Marks the issue as resolved.
    /// </summary>
    public void Resolve(Guid userId, string? notes = null) {
        Status = IssueStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolvedByUserId = userId;
        ResolutionNotes = notes;
    }

    /// <summary>
    /// Reopens a resolved issue.
    /// </summary>
    public void Reopen() {
        Status = IssueStatus.Unresolved;
        ResolvedAt = null;
        ResolvedByUserId = null;
        ResolutionNotes = null;
    }

    /// <summary>
    /// Ignores the issue (won't trigger alerts).
    /// </summary>
    public void Ignore() {
        Status = IssueStatus.Ignored;
    }

    /// <summary>
    /// Mutes alerts for this issue temporarily.
    /// </summary>
    public void Mute(TimeSpan duration) {
        IsMuted = true;
        MutedUntil = DateTime.UtcNow.Add(duration);
    }

    /// <summary>
    /// Unmutes alerts for this issue.
    /// </summary>
    public void Unmute() {
        IsMuted = false;
        MutedUntil = null;
    }
}

/// <summary>
/// Represents the status of an error issue.
/// </summary>
public enum IssueStatus {
    Unresolved = 0,
    Resolved = 1,
    Ignored = 2,
    InProgress = 3,
    Regressed = 4
}
