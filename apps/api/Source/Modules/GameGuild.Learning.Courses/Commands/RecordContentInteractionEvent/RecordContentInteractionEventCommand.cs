using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

public sealed record RecordContentInteractionEventCommand(
    Guid ProgramId,
    Guid InteractionId,
    ContentInteractionEventType Type,
    int? DurationSeconds = null,
    decimal? PositionSeconds = null,
    decimal? ProgressPercentage = null,
    string? Payload = null,
    string? IdempotencyKey = null,
    DateTime? OccurredAt = null) : ICommand<ContentInteractionEventDto>;
