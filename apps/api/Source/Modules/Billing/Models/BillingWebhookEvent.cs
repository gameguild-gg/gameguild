using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Core.Entities;

namespace GameGuild.Modules.Billing.Models;

/// <summary>
/// Represents a webhook event received from a billing provider
/// </summary>
[Table("BillingWebhookEvents")]
public class BillingWebhookEvent : EntityBase
{
  /// <summary>
  /// Payment provider that sent the webhook (stripe, paypal, etc.)
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string Provider { get; set; } = string.Empty;

  /// <summary>
  /// External event ID from the provider
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string ExternalEventId { get; set; } = string.Empty;

  /// <summary>
  /// Type of webhook event (subscription.created, payment.succeeded, etc.)
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string EventType { get; set; } = string.Empty;

  /// <summary>
  /// Raw webhook payload as received
  /// </summary>
  [Required]
  public string Payload { get; set; } = string.Empty;

  /// <summary>
  /// Headers received with the webhook
  /// </summary>
  public string? Headers { get; set; }

  /// <summary>
  /// Whether the webhook has been processed successfully
  /// </summary>
  public bool IsProcessed { get; set; }

  /// <summary>
  /// Whether the webhook processing failed
  /// </summary>
  public bool IsFailed { get; set; }

  /// <summary>
  /// Number of processing attempts
  /// </summary>
  public int ProcessingAttempts { get; set; }

  /// <summary>
  /// Error message if processing failed
  /// </summary>
  public string? ErrorMessage { get; set; }

  /// <summary>
  /// When the webhook was processed
  /// </summary>
  public DateTime? ProcessedAt { get; set; }

  /// <summary>
  /// Related tenant ID if applicable
  /// </summary>
  public Guid? TenantId { get; set; }

  /// <summary>
  /// Related subscription ID if applicable
  /// </summary>
  public Guid? SubscriptionId { get; set; }

  /// <summary>
  /// Related user ID if applicable
  /// </summary>
  public Guid? UserId { get; set; }

  /// <summary>
  /// Mark webhook as processed
  /// </summary>
  public void MarkAsProcessed()
  {
    IsProcessed = true;
    ProcessedAt = DateTime.UtcNow;
    IsFailed = false;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary>
  /// Mark webhook as failed
  /// </summary>
  public void MarkAsFailed(string errorMessage)
  {
    IsFailed = true;
    ErrorMessage = errorMessage;
    ProcessingAttempts++;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary>
  /// Increment processing attempts
  /// </summary>
  public void IncrementAttempts()
  {
    ProcessingAttempts++;
    UpdatedAt = DateTime.UtcNow;
  }
}
