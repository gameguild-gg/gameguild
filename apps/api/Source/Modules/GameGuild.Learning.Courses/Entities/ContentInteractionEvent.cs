using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

[Table("content_interaction_events")]
[Index(nameof(InteractionId), nameof(OccurredAt))]
[Index(nameof(InteractionId), nameof(IdempotencyKey), IsUnique = true)]
public sealed class ContentInteractionEvent : EntityBase
{
    [Required]
    public Guid InteractionId { get; set; }

    public ContentInteractionEventType Type { get; set; }

    public DateTime OccurredAt { get; set; }

    public int? DurationSeconds { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal? PositionSeconds { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? ProgressPercentage { get; set; }

    public string? Payload { get; set; }

    [MaxLength(128)]
    public string? IdempotencyKey { get; set; }

    public ContentInteraction Interaction { get; set; } = null!;

    public static ContentInteractionEvent Create(
        Guid interactionId,
        ContentInteractionEventType type,
        int? durationSeconds = null,
        decimal? positionSeconds = null,
        decimal? progressPercentage = null,
        string? payload = null,
        string? idempotencyKey = null,
        DateTime? occurredAt = null)
    {
        if (interactionId == Guid.Empty)
        {
            throw new ArgumentException("Interaction ID is required.", nameof(interactionId));
        }

        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be positive when provided.");
        }

        if (positionSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(positionSeconds), "Position cannot be negative.");
        }

        if (progressPercentage is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(progressPercentage), "Progress must be between 0 and 100.");
        }

        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                using var _ = JsonDocument.Parse(payload);
            }
            catch (JsonException exception)
            {
                throw new ArgumentException("Event payload must be valid JSON.", nameof(payload), exception);
            }
        }

        var normalizedIdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim();
        if (normalizedIdempotencyKey?.Length > 128)
        {
            throw new ArgumentException("Idempotency key cannot exceed 128 characters.", nameof(idempotencyKey));
        }

        return new ContentInteractionEvent
        {
            Id = Guid.NewGuid(),
            InteractionId = interactionId,
            Type = type,
            OccurredAt = (occurredAt ?? SystemClock.UtcNow).ToUniversalTime(),
            DurationSeconds = durationSeconds,
            PositionSeconds = positionSeconds,
            ProgressPercentage = progressPercentage,
            Payload = string.IsNullOrWhiteSpace(payload) ? null : payload,
            IdempotencyKey = normalizedIdempotencyKey,
        };
    }
}
