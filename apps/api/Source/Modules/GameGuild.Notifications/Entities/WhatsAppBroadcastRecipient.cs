using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Notifications;

/// <summary>
///     Per-recipient delivery tracking for a <see cref="WhatsAppBroadcast"/> (issue #55):
///     one row per resolved audience member, recording the destination phone, the
///     provider message id on acceptance, and the per-recipient outcome used for
///     campaign success reporting.
/// </summary>
[Table("WhatsAppBroadcastRecipients")]
[Index(nameof(BroadcastId))]
[Index(nameof(BroadcastId), nameof(Status))]
[Index(nameof(UserId))]
public class WhatsAppBroadcastRecipient : EntityBase
{
    /// <summary>Parent campaign.</summary>
    public Guid BroadcastId { get; private set; }

    /// <summary>Denormalized tenant scope inherited from the parent campaign.</summary>
    public Guid RecipientTenantId { get; private set; }

    /// <summary>Audience member receiving the broadcast.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Destination phone number in E.164 format as resolved at send time.</summary>
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>Per-recipient delivery outcome.</summary>
    public WhatsAppBroadcastDeliveryStatus Status { get; private set; } = WhatsAppBroadcastDeliveryStatus.Pending;

    /// <summary>WhatsApp-assigned message id (WAMID) for delivery tracking.</summary>
    [MaxLength(100)]
    public string? ProviderMessageId { get; private set; }

    /// <summary>Last delivery error for this recipient, if any.</summary>
    [MaxLength(1000)]
    public string? Error { get; private set; }

    /// <summary>Number of send attempts made for this recipient.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>When the provider accepted the message for this recipient.</summary>
    public DateTime? SentAtUtc { get; private set; }

    private WhatsAppBroadcastRecipient() { }

    /// <summary>Creates a pending recipient row.</summary>
    public static WhatsAppBroadcastRecipient Create(
        Guid broadcastId,
        Guid tenantId,
        Guid userId,
        string phoneNumber)
    {
        return new WhatsAppBroadcastRecipient
        {
            Id = Guid.NewGuid(),
            BroadcastId = broadcastId,
            RecipientTenantId = tenantId,
            UserId = userId,
            PhoneNumber = phoneNumber,
            Status = WhatsAppBroadcastDeliveryStatus.Pending
        };
    }

    /// <summary>Records an accepted send for this recipient.</summary>
    public void MarkSent(string providerMessageId)
    {
        Status = WhatsAppBroadcastDeliveryStatus.Sent;
        ProviderMessageId = providerMessageId;
        Error = null;
        SentAtUtc = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>Records a rejected send for this recipient.</summary>
    public void MarkFailed(string error)
    {
        Status = WhatsAppBroadcastDeliveryStatus.Failed;
        Error = error.Length <= 1000 ? error : error[..1000];
        AttemptCount++;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Records that this recipient was skipped without a send attempt (for example
    ///     opted out of the channel or the marketing category).
    /// </summary>
    public void MarkSkipped(string reason)
    {
        Status = WhatsAppBroadcastDeliveryStatus.Skipped;
        Error = reason.Length <= 1000 ? reason : reason[..1000];
        UpdatedAt = SystemClock.UtcNow;
    }
}

/// <summary>Per-recipient delivery outcomes.</summary>
public enum WhatsAppBroadcastDeliveryStatus
{
    /// <summary>Resolved audience member awaiting the send sweep.</summary>
    Pending = 0,

    /// <summary>Provider accepted the message.</summary>
    Sent = 1,

    /// <summary>Provider rejected the message.</summary>
    Failed = 2,

    /// <summary>Deliberately not sent (opted out or missing phone).</summary>
    Skipped = 3
}
