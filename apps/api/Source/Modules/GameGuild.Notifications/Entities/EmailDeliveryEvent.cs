using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Notifications.Services.Email;

namespace GameGuild.Notifications;

/// <summary>
/// A provider-side email delivery event (send, delivery, bounce, complaint, open)
/// ingested from SES via SNS. Correlation to <see cref="Notification"/> rows is via
/// <see cref="ProviderMessageId"/> join — events deliberately carry no NotificationId
/// (provider events arrive uncorrelated on our side).
/// </summary>
[Table("EmailDeliveryEvents")]
public class EmailDeliveryEvent : EntityBase
{
    /// <summary>
    /// Message id assigned by the email provider (SES outbound MessageId); joins to
    /// Notification.ProviderMessageId and groups the event timeline of one email.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ProviderMessageId { get; private set; } = string.Empty;

    /// <summary>
    /// Normalized recipient address (see <see cref="EmailAddressNormalizer"/>)
    /// </summary>
    [Required]
    [MaxLength(320)]
    public string RecipientEmail { get; private set; } = string.Empty;

    /// <summary>
    /// What happened to the email at the provider
    /// </summary>
    [Required]
    public EmailDeliveryEventType EventType { get; private set; }

    /// <summary>
    /// When the event occurred at the provider (SES timestamp — NOT CreatedAt, which is ingest time)
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Bounce classification when EventType is Bounce (e.g. Permanent, Transient, Undetermined)
    /// </summary>
    [MaxLength(30)]
    public string? BounceType { get; private set; }

    /// <summary>
    /// Provider diagnostic code for bounces (e.g. 5.1.1 / 4.2.1 SMTP codes)
    /// </summary>
    [MaxLength(200)]
    public string? DiagnosticCode { get; private set; }

    /// <summary>
    /// SNS message id — unique per delivery attempt of an SNS notification; enforces idempotent ingest
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SnsMessageId { get; private set; } = string.Empty;

    /// <summary>
    /// Privacy-stripped raw event payload as JSON (ipAddress/userAgent removed by the
    /// ingesting webhook; truncated to 4000 chars — wrapped as valid JSON — before save).
    /// </summary>
    public string? Payload { get; private set; }

    /// <summary>
    /// EF Core constructor
    /// </summary>
    private EmailDeliveryEvent() { }

    /// <summary>
    /// Creates a new delivery event. The recipient address is normalized via
    /// <see cref="EmailAddressNormalizer.Normalize"/> so stored values always match
    /// suppression lookups.
    /// </summary>
    public static EmailDeliveryEvent Create(
        string providerMessageId,
        string recipientEmail,
        EmailDeliveryEventType eventType,
        DateTime occurredAt,
        string snsMessageId,
        string? bounceType = null,
        string? diagnosticCode = null,
        string? payload = null)
    {
        return new EmailDeliveryEvent
        {
            ProviderMessageId = providerMessageId,
            RecipientEmail = EmailAddressNormalizer.Normalize(recipientEmail),
            EventType = eventType,
            OccurredAt = occurredAt,
            SnsMessageId = snsMessageId,
            BounceType = bounceType,
            DiagnosticCode = diagnosticCode,
            Payload = payload
        };
    }
}

/// <summary>
/// Provider delivery event types tracked by the platform
/// </summary>
public enum EmailDeliveryEventType
{
    /// <summary>Provider accepted the send request</summary>
    Send = 0,

    /// <summary>Provider delivered the email to the recipient mailbox</summary>
    Delivery = 1,

    /// <summary>Recipient mail server rejected the email</summary>
    Bounce = 2,

    /// <summary>Recipient marked the email as spam</summary>
    Complaint = 3,

    /// <summary>Recipient opened the email (tracking pixel)</summary>
    Open = 4
}
