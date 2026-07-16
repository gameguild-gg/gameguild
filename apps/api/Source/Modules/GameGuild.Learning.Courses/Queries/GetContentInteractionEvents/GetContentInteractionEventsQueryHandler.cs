using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

public sealed class GetContentInteractionEventsQueryHandler(
    IApplicationDbContext context,
    IRequestContextAccessor requestContextAccessor)
    : IQueryHandler<GetContentInteractionEventsQuery, IReadOnlyList<ContentInteractionEventDto>>
{
    public async Task<IReadOnlyList<ContentInteractionEventDto>> Handle(
        GetContentInteractionEventsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = requestContextAccessor.CurrentUserId;
        if (!currentUserId.HasValue)
        {
            throw new RequestValidationException("Content interaction was not found in this course.");
        }

        var interactionBelongsToCourse = await context.Set<ContentInteraction>()
            .AnyAsync(
                item => item.Id == request.InteractionId &&
                        item.UserId == currentUserId.Value &&
                        item.Content.ProgramId == request.ProgramId,
                cancellationToken)
            .ConfigureAwait(false);
        if (!interactionBelongsToCourse)
        {
            throw new RequestValidationException("Content interaction was not found in this course.");
        }

        var events = await context.Set<ContentInteractionEvent>()
            .AsNoTracking()
            .Where(item => item.InteractionId == request.InteractionId)
            .OrderBy(item => item.OccurredAt)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return events.Select(ContentInteractionEventDto.FromEntity).ToList();
    }
}
