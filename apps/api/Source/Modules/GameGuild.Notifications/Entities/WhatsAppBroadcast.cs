using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Notifications;

/// <summary>
///     A WhatsApp broadcast campaign (issue #55): one admin-authored template message
///     delivered to a filtered segment of a tenant's members. Tenant-scoped by
///     construction (the inherited <see cref="EntityBase.TenantId"/> is required),
///     bounded by the configured maximum recipient count, and only sendable through
///     templates on the configured allowlist (WhatsApp requires pre-approved templates
///     for business-initiated conversations).
/// </summary>
[Table("WhatsAppBroadcasts")]
[Index(nameof(TenantId), nameof(Status))]
[Index(nameof(TenantId), nameof(CreatedAt))]
[Index(nameof(ScheduledAtUtc))]
public class WhatsAppBroadcast : EntityBase
{
    /// <summary>Admin-facing label for the campaign.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; private set; } = string.Empty;

    /// <summary>Allowlisted WhatsApp template name used for every recipient.</summary>
    [Required]
    [MaxLength(100)]
    public string TemplateName { get; private set; } = string.Empty;

    /// <summary>Template language code; defaults to en_US.</summary>
    [MaxLength(10)]
    public string LanguageCode { get; private set; } = "en_US";

    /// <summary>
    ///     Optional JSON object of shared template variables applied to every recipient
    ///     (keys sorted to template positions by the provider).
    /// </summary>
    [MaxLength(4000)]
    public string? VariablesJson { get; private set; }

    /// <summary>Optional TenantMember role filter narrowing the audience segment.</summary>
    [MaxLength(100)]
    public string? SegmentRole { get; private set; }

    /// <summary>Whether the audience is limited to members with an active tenant membership.</summary>
    public bool SegmentActiveMembersOnly { get; private set; } = true;

    /// <summary>Campaign lifecycle state.</summary>
    public WhatsAppBroadcastStatus Status { get; private set; } = WhatsAppBroadcastStatus.Draft;

    /// <summary>When a scheduled campaign becomes sendable; null for immediate sends.</summary>
    public DateTime? ScheduledAtUtc { get; private set; }

    /// <summary>When the campaign finished sending (all recipients attempted).</summary>
    public DateTime? SentAtUtc { get; private set; }

    /// <summary>Number of recipients recorded for the send (bounded by options).</summary>
    public int RecipientCount { get; private set; }

    /// <summary>Admin user who created the campaign.</summary>
    public Guid CreatedBy { get; private set; }

    /// <summary>Last send-level error (used when the campaign fails before per-recipient delivery).</summary>
    [MaxLength(1000)]
    public string? Error { get; private set; }

    private WhatsAppBroadcast() { }

    /// <summary>Creates a campaign in the Draft state.</summary>
    public static WhatsAppBroadcast Create(
        Guid tenantId,
        string title,
        string templateName,
        string? languageCode,
        string? variablesJson,
        string? segmentRole,
        bool segmentActiveMembersOnly,
        Guid createdBy)
    {
        return new WhatsAppBroadcast
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            TemplateName = templateName,
            LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "en_US" : languageCode.Trim(),
            VariablesJson = variablesJson,
            SegmentRole = string.IsNullOrWhiteSpace(segmentRole) ? null : segmentRole.Trim(),
            SegmentActiveMembersOnly = segmentActiveMembersOnly,
            Status = WhatsAppBroadcastStatus.Draft,
            CreatedBy = createdBy
        };
    }

    /// <summary>Schedules the campaign for a future send time (Draft only).</summary>
    public void Schedule(DateTime scheduledAtUtc)
    {
        if (Status != WhatsAppBroadcastStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft broadcasts can be scheduled (current status: {Status}).");
        }

        if (scheduledAtUtc <= SystemClock.UtcNow)
        {
            throw new InvalidOperationException("The scheduled time must be in the future.");
        }

        Status = WhatsAppBroadcastStatus.Scheduled;
        ScheduledAtUtc = scheduledAtUtc;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Claims the campaign for sending (idempotent for the scheduler's re-claim
    ///     window; rejects terminal and already-sent states).
    /// </summary>
    public void MarkSending()
    {
        if (Status is not (WhatsAppBroadcastStatus.Scheduled or WhatsAppBroadcastStatus.Draft))
        {
            throw new InvalidOperationException($"This broadcast cannot be sent in its current state: {Status}.");
        }

        Status = WhatsAppBroadcastStatus.Sending;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>Records the resolved audience size when recipient rows are created.</summary>
    public void RecordRecipients(int count)
    {
        RecipientCount = count;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>Completes the campaign after every recipient row has been attempted.</summary>
    public void MarkSent()
    {
        Status = WhatsAppBroadcastStatus.Sent;
        SentAtUtc = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>Fails the campaign with a campaign-level error (recipients keep their own outcomes).</summary>
    public void MarkFailed(string error)
    {
        Status = WhatsAppBroadcastStatus.Failed;
        Error = error.Length <= 1000 ? error : error[..1000];
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>Cancels a not-yet-sent campaign.</summary>
    public void Cancel()
    {
        if (Status is not (WhatsAppBroadcastStatus.Draft or WhatsAppBroadcastStatus.Scheduled))
        {
            throw new InvalidOperationException($"Only draft or scheduled broadcasts can be cancelled (current status: {Status}).");
        }

        Status = WhatsAppBroadcastStatus.Cancelled;
        UpdatedAt = SystemClock.UtcNow;
    }
}

/// <summary>Campaign lifecycle states.</summary>
public enum WhatsAppBroadcastStatus
{
    /// <summary>Authored, not yet scheduled or sent.</summary>
    Draft = 0,

    /// <summary>Waiting for the scheduled time; picked up by the scheduler sweep.</summary>
    Scheduled = 1,

    /// <summary>Recipients resolved; delivery in progress.</summary>
    Sending = 2,

    /// <summary>Every recipient row has been attempted.</summary>
    Sent = 3,

    /// <summary>Campaign-level failure before per-recipient delivery completed.</summary>
    Failed = 4,

    /// <summary>Cancelled by an admin before sending.</summary>
    Cancelled = 5
}
