using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

public sealed record GetContentInteractionEventsQuery(Guid ProgramId, Guid InteractionId)
    : IQuery<IReadOnlyList<ContentInteractionEventDto>>;
