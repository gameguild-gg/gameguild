using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.CQRS.Models;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Notifications;

/// <summary>
/// Represents a notification sent to a user
/// </summary>
[Table("Notifications")]
[Index(nameof(RecipientId), nameof(IsRead))]
[Index(nameof(RecipientId), nameof(CreatedAt))]
[Index(nameof(Type))]
[Index(nameof(Channel))]
[Index(nameof(ScheduledAt))]
public class Notification : EntityBase
{
    /// <summary>
    /// ID of the user who will receive this notification.
    /// Null for email-only recipients (e.g., tenant invites to unregistered addresses).
    /// </summary>
    public Guid? RecipientId { get; private set; }

    /// <summary>
    /// Tenant context for this notification
    /// </summary>
    [NotMapped]
    public TenantId? NotificationTenantId => TenantId.HasValue ? new TenantId(TenantId.Value) : null;

    /// <summary>
    /// The type of notification (e.g., CourseEnrollment, AchievementUnlocked, etc.)
    /// </summary>
    [Required]
    public NotificationType Type { get; private set; }

    /// <summary>
    /// The delivery channel for this notification
    /// </summary>
    [Required]
    public NotificationChannel Channel { get; private set; }

    /// <summary>
    /// Short title for the notification
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Full notification message content
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Optional URL to link when the notification is clicked
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrl { get; private set; }

    /// <summary>
    /// Optional icon or image URL for the notification
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; private set; }

    /// <summary>
    /// Whether the notification has been read by the recipient
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// When the notification was read
    /// </summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>
    /// Whether the notification has been sent/delivered
    /// </summary>
    public bool IsSent { get; private set; }

    /// <summary>
    /// When the notification was sent
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// Delivery pipeline state for out-of-band channels (email dispatcher, digest engine)
    /// </summary>
    public NotificationDeliveryStatus DeliveryStatus { get; private set; } = NotificationDeliveryStatus.Pending;

    /// <summary>
    /// Number of delivery attempts made for this notification
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// Last delivery error, if any
    /// </summary>
    [MaxLength(1000)]
    public string? LastError { get; private set; }

    /// <summary>
    /// When the next delivery attempt should be made (retry backoff / quiet-hours hold)
    /// </summary>
    public DateTime? NextAttemptAt { get; private set; }

    /// <summary>
    /// Email address for email-channel notifications when no user account exists
    /// </summary>
    [MaxLength(320)]
    public string? RecipientEmail { get; private set; }

    /// <summary>
    /// Optional scheduled delivery time (for delayed notifications)
    /// </summary>
    public DateTime? ScheduledAt { get; private set; }

    /// <summary>
    /// Priority level of the notification
    /// </summary>
    public NotificationPriority Priority { get; private set; } = NotificationPriority.Normal;

    /// <summary>
    /// Optional reference to the entity that triggered this notification
    /// </summary>
    public Guid? ReferenceEntityId { get; private set; }

    /// <summary>
    /// Optional type of the entity that triggered this notification
    /// </summary>
    [MaxLength(100)]
    public string? ReferenceEntityType { get; private set; }

    /// <summary>
    /// Optional metadata JSON for additional notification data
    /// </summary>
    [MaxLength(4000)]
    public string? Metadata { get; private set; }

    /// <summary>
    /// Notification template ID if created from a template
    /// </summary>
    public Guid? TemplateId { get; private set; }

    /// <summary>
    /// Navigation property to the template
    /// </summary>
    [ForeignKey(nameof(TemplateId))]
    public virtual NotificationTemplate? Template { get; private set; }

    /// <summary>
    /// EF Core constructor
    /// </summary>
    private Notification() { }

    /// <summary>
    /// Creates a new notification
    /// </summary>
    public static Notification Create(
        Guid? recipientId,
        NotificationType type,
        NotificationChannel channel,
        string title,
        string message,
        Guid? tenantId = null,
        string? actionUrl = null,
        string? iconUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        DateTime? scheduledAt = null,
        Guid? referenceEntityId = null,
        string? referenceEntityType = null,
        string? metadata = null,
        Guid? templateId = null,
        string? recipientEmail = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            RecipientEmail = recipientEmail,
            TenantId = tenantId,
            Type = type,
            Channel = channel,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            IconUrl = iconUrl,
            Priority = priority,
            ScheduledAt = scheduledAt,
            ReferenceEntityId = referenceEntityId,
            ReferenceEntityType = referenceEntityType,
            Metadata = metadata,
            TemplateId = templateId,
            IsRead = false,
            IsSent = false
        };
    }

    /// <summary>
    /// Marks the notification as read
    /// </summary>
    public void MarkAsRead()
    {
        if (IsRead) return;
        
        IsRead = true;
        ReadAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Marks the notification as unread
    /// </summary>
    public void MarkAsUnread()
    {
        IsRead = false;
        ReadAt = null;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Marks the notification as sent
    /// </summary>
    public void MarkAsSent()
    {
        if (IsSent) return;
        
        IsSent = true;
        SentAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Marks the delivery pipeline state as Sent (idempotent; also sets IsSent/SentAt)
    /// </summary>
    public void MarkDeliverySent()
    {
        MarkAsSent();
        if (DeliveryStatus == NotificationDeliveryStatus.Sent) return;

        DeliveryStatus = NotificationDeliveryStatus.Sent;
        NextAttemptAt = null;
        LastError = null;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Records a failed delivery attempt and schedules the next retry (status returns to Pending)
    /// </summary>
    public void MarkDeliveryAttemptFailed(string error, DateTime nextAttemptAt)
    {
        AttemptCount++;
        LastError = error;
        NextAttemptAt = nextAttemptAt;
        DeliveryStatus = NotificationDeliveryStatus.Pending;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Gives up on delivery after exhausting retries or exceeding the staleness TTL
    /// </summary>
    public void MarkDeadLettered(string reason)
    {
        DeliveryStatus = NotificationDeliveryStatus.DeadLettered;
        LastError = reason;
        NextAttemptAt = null;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Atomically claims a Pending notification for the sender (no-op unless Pending)
    /// </summary>
    public void ClaimForSending()
    {
        if (DeliveryStatus != NotificationDeliveryStatus.Pending) return;

        DeliveryStatus = NotificationDeliveryStatus.Sending;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Quarantines the notification for a future digest email
    /// </summary>
    public void MarkHeldForDigest()
    {
        DeliveryStatus = NotificationDeliveryStatus.HeldForDigest;
        NextAttemptAt = null;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Soft deletes the notification
    /// </summary>
    public void Delete() => SoftDelete();
}

/// <summary>
/// Types of notifications that can be sent
/// </summary>
public enum NotificationType
{
    /// <summary>General system notification</summary>
    System = 0,

    /// <summary>User was enrolled in a course</summary>
    CourseEnrollment = 1,

    /// <summary>Course was completed</summary>
    CourseCompletion = 2,

    /// <summary>Achievement was unlocked</summary>
    AchievementUnlocked = 3,

    /// <summary>Certificate was issued</summary>
    CertificateIssued = 4,

    /// <summary>New content is available</summary>
    NewContent = 5,

    /// <summary>Assessment is due or deadline approaching</summary>
    AssessmentReminder = 6,

    /// <summary>Assessment was graded</summary>
    AssessmentGraded = 7,

    /// <summary>Cohort activity notification</summary>
    CohortActivity = 8,

    /// <summary>Social interaction (like, comment, follow)</summary>
    SocialInteraction = 9,

    /// <summary>Direct message received</summary>
    DirectMessage = 10,

    /// <summary>Payment or subscription related</summary>
    Billing = 11,

    /// <summary>Security alert (password change, login from new device)</summary>
    Security = 12,

    /// <summary>Marketing or promotional notification</summary>
    Marketing = 13,

    /// <summary>Account verification or onboarding</summary>
    Onboarding = 14,

    /// <summary>Feature announcement</summary>
    FeatureAnnouncement = 15,

    /// <summary>Recommendation for content</summary>
    Recommendation = 16,

    /// <summary>Streak or progress milestone</summary>
    ProgressMilestone = 17,

    /// <summary>Inactivity reminder</summary>
    InactivityReminder = 18,

    /// <summary>Email address verification</summary>
    EmailVerification = 19,

    /// <summary>Password reset request</summary>
    PasswordReset = 20,

    /// <summary>Sign-in magic link</summary>
    MagicLink = 21,

    /// <summary>Tenant membership invitation</summary>
    TenantInvite = 22,

    /// <summary>Monthly billing statement</summary>
    MonthlyStatement = 23,

    /// <summary>Custom/other notification type</summary>
    Custom = 99
}

/// <summary>
/// Delivery pipeline state for out-of-band channels
/// </summary>
public enum NotificationDeliveryStatus
{
    /// <summary>Waiting to be picked up by the dispatcher</summary>
    Pending = 0,

    /// <summary>Claimed by a sender; delivery in progress</summary>
    Sending = 1,

    /// <summary>Delivered successfully</summary>
    Sent = 2,

    /// <summary>Delivery failed; will be retried</summary>
    Failed = 3,

    /// <summary>Permanently failed after exhausting retries or exceeding TTL</summary>
    DeadLettered = 4,

    /// <summary>Quarantined for a future digest email</summary>
    HeldForDigest = 5
}

/// <summary>
/// Delivery channels for notifications
/// </summary>
public enum NotificationChannel
{
    /// <summary>In-app notification (default)</summary>
    InApp = 0,

    /// <summary>Email notification</summary>
    Email = 1,

    /// <summary>Push notification (mobile/web)</summary>
    Push = 2,

    /// <summary>SMS notification</summary>
    Sms = 3,

    /// <summary>Slack integration</summary>
    Slack = 4,

    /// <summary>Discord integration</summary>
    Discord = 5,

    /// <summary>Webhook notification</summary>
    Webhook = 6
}

/// <summary>
/// Priority levels for notifications
/// </summary>
public enum NotificationPriority
{
    /// <summary>Low priority - can be batched or delayed</summary>
    Low = 0,

    /// <summary>Normal priority - delivered promptly</summary>
    Normal = 1,

    /// <summary>High priority - delivered immediately</summary>
    High = 2,

    /// <summary>Urgent - highest priority, may bypass user preferences</summary>
    Urgent = 3
}
