using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Notifications.Services.Email;

namespace GameGuild.Notifications;

/// <summary>
/// A suppressed email address — blocked from future sends after a hard bounce or
/// spam complaint. Platform-level (TenantId null); active while <see cref="ReleasedAt"/> is null.
/// </summary>
[Table("EmailSuppressions")]
public class EmailSuppression : EntityBase
{
    /// <summary>
    /// Normalized email address (see <see cref="EmailAddressNormalizer"/>); unique across the table
    /// </summary>
    [Required]
    [MaxLength(320)]
    public string EmailAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Why the address was suppressed
    /// </summary>
    [Required]
    public EmailSuppressionReason Reason { get; private set; }

    /// <summary>
    /// Bounce classification when Reason is HardBounce (e.g. Permanent, Undetermined)
    /// </summary>
    [MaxLength(30)]
    public string? BounceType { get; private set; }

    /// <summary>
    /// The EmailDeliveryEvent id that triggered this suppression, for audit
    /// </summary>
    public Guid? SourceEventId { get; private set; }

    /// <summary>
    /// When the address was (re-)suppressed
    /// </summary>
    public DateTime SuppressedAt { get; private set; }

    /// <summary>
    /// When an admin released the suppression; null while the suppression is active
    /// </summary>
    public DateTime? ReleasedAt { get; private set; }

    /// <summary>
    /// Whether sends to this address are currently blocked
    /// </summary>
    public bool IsActive => ReleasedAt == null;

    /// <summary>
    /// EF Core constructor
    /// </summary>
    private EmailSuppression() { }

    /// <summary>
    /// Creates a new suppression. The address is normalized via
    /// <see cref="EmailAddressNormalizer.Normalize"/> so stored values always match
    /// lookups from the dispatchers and event processor.
    /// </summary>
    public static EmailSuppression Create(
        string emailAddress,
        EmailSuppressionReason reason,
        string? bounceType = null,
        Guid? sourceEventId = null)
    {
        return new EmailSuppression
        {
            EmailAddress = EmailAddressNormalizer.Normalize(emailAddress),
            Reason = reason,
            BounceType = bounceType,
            SourceEventId = sourceEventId,
            SuppressedAt = SystemClock.UtcNow
        };
    }

    /// <summary>
    /// Releases the suppression so the address can receive emails again (admin unsuppress)
    /// </summary>
    public void Release()
    {
        ReleasedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }
}

/// <summary>
/// Reasons an email address can be suppressed
/// </summary>
public enum EmailSuppressionReason
{
    /// <summary>Permanent/undetermined bounce — mailbox does not exist or rejects mail</summary>
    HardBounce = 0,

    /// <summary>Recipient marked an email as spam</summary>
    Complaint = 1
}
