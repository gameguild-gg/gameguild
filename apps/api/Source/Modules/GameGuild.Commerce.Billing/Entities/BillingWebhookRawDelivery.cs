using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Redacted raw delivery of an inbound provider webhook (issue #408: capture raw
///     payload and all headers). Persisted ONLY when the opt-in retention policy
///     (<see cref="WebhookRetentionPolicy"/>) is enabled, ONLY after the provider signature
///     was verified, and ONLY through the one-way
///     <see cref="WebhookRawDeliveryRedactor"/> pass: signature/authorization/token values
///     are stored as SHA-256 hashes (verifiable-by-hash against a candidate raw value) and
///     email addresses as deterministic tokens. One row per durable inbox row, purged by
///     the cleanup worker once <see cref="ExpiresAt"/> elapses.
/// </summary>
[Table("BillingWebhookRawDeliveries")]
public class BillingWebhookRawDelivery : EntityBase
{
    /// <summary>
    ///     Durable inbox row this raw delivery was accepted alongside.
    /// </summary>
    public Guid BillingWebhookEventId { get; set; }

    /// <summary>
    ///     Payment provider that sent the delivery (denormalized from the inbox row so the
    ///     raw evidence stays self-describing).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    ///     External event ID from the provider (denormalized from the inbox row).
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ExternalEventId { get; set; } = string.Empty;

    /// <summary>
    ///     The raw HTTP body of the delivery in its REDACTED form. Never contains literal
    ///     secrets; emails appear as deterministic tokens when PII redaction is on.
    /// </summary>
    [Required]
    public string RawPayload { get; set; } = string.Empty;

    /// <summary>
    ///     The full header set of the delivery as redacted JSON. Secret-bearing headers
    ///     hash to <c>sha256:&lt;hex&gt;</c> (verifiable-by-hash); the rest stay verbatim.
    /// </summary>
    [Required]
    public string RawHeaders { get; set; } = string.Empty;

    /// <summary>
    ///     SHA-256 of the ORIGINAL raw payload bytes (hex, lowercase) — intake evidence
    ///     computed before redaction, comparable against the minimized inbox row's payload
    ///     hash and against the provider's own delivery signature material.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PayloadSha256 { get; set; } = string.Empty;

    /// <summary>
    ///     Whether the persisted form was produced with PII redaction enabled.
    /// </summary>
    public bool PiiRedacted { get; set; }

    /// <summary>
    ///     When the delivery was accepted (intake time, before processing).
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    ///     Retention deadline: the cleanup worker purges the row once this elapses.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
