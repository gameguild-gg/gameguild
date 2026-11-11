using GameGuild.CQRS;
using GameGuild.Learning;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GameGuild.Learning.Courses;

public sealed class RecordContentInteractionEventCommandHandler(
    IApplicationDbContext context,
    IRequestContextAccessor requestContextAccessor,
    IPublisher? publisher = null)
    : ICommandHandler<RecordContentInteractionEventCommand, ContentInteractionEventDto>
{
    public async Task<ContentInteractionEventDto> Handle(
        RecordContentInteractionEventCommand request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();
        var currentUserId = requestContextAccessor.CurrentUserId;
        if (!currentUserId.HasValue)
        {
            throw new RequestValidationException("Content interaction was not found in this course.");
        }

        var interaction = await context.Set<ContentInteraction>()
            .Include(item => item.Content)
            .FirstOrDefaultAsync(
                item => item.Id == request.InteractionId && item.UserId == currentUserId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (interaction is null || interaction.Content.ProgramId != request.ProgramId)
        {
            throw new RequestValidationException("Content interaction was not found in this course.");
        }

        if (idempotencyKey is not null)
        {
            var existing = await FindExistingEventAsync(
                request.InteractionId,
                idempotencyKey,
                cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                EnsureMatchingReplay(existing, request);
                return ContentInteractionEventDto.FromEntity(existing);
            }
        }

        if (ProgramContentMappingExtensions.NormalizeProfessorFacingType(interaction.Content.Type) !=
            ProgramContentType.Lesson)
        {
            throw new RequestValidationException("Fine-grained interaction events are only supported for lessons.");
        }

        if (interaction.SubmittedAt.HasValue)
        {
            throw new RequestValidationException("Submitted interactions cannot receive new events.");
        }

        if (request.Type == ContentInteractionEventType.Heartbeat && !request.DurationSeconds.HasValue)
        {
            throw new RequestValidationException("Heartbeat events require a duration in seconds.");
        }

        var item = ContentInteractionEvent.Create(
            request.InteractionId,
            request.Type,
            request.DurationSeconds,
            request.PositionSeconds,
            request.ProgressPercentage,
            request.Payload,
            idempotencyKey,
            request.OccurredAt);

        ApplyEvent(interaction, item);
        context.Set<ContentInteractionEvent>().Add(item);
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException) when (idempotencyKey is not null)
        {
            context.Set<ContentInteractionEvent>().Remove(item);
            var existing = await FindExistingEventAsync(
                request.InteractionId,
                idempotencyKey,
                cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                EnsureMatchingReplay(existing, request);
                return ContentInteractionEventDto.FromEntity(existing);
            }

            throw;
        }

        // Issue #1184 G4: ContentStartedEvent/ContentCompletedEvent were declared but never
        // raised; fine-grained interaction events are their producing sites (Opened starts a
        // content item, Completed finishes it). Replays returned above never reach this point.
        // Separate method so the publishes stay outside the persistence method (GGARCH003).
        await PublishContentLifecycleAsync(currentUserId.Value, request.ProgramId, interaction, item, cancellationToken).ConfigureAwait(false);

        return ContentInteractionEventDto.FromEntity(item);
    }

    private async Task PublishContentLifecycleAsync(
        Guid userId,
        Guid programId,
        ContentInteraction interaction,
        ContentInteractionEvent item,
        CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        var contentType = interaction.Content.Type.ToString();
        if (item.Type == ContentInteractionEventType.Opened)
        {
            await publisher.Publish(
                new ContentStartedEvent(userId, programId, interaction.ContentId, null, contentType),
                cancellationToken).ConfigureAwait(false);
        }

        if (item.Type == ContentInteractionEventType.Completed)
        {
            await publisher.Publish(
                new ContentCompletedEvent(
                    userId,
                    programId,
                    interaction.ContentId,
                    null,
                    contentType,
                    item.DurationSeconds ?? 0),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<ContentInteractionEvent?> FindExistingEventAsync(
        Guid interactionId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        context.Set<ContentInteractionEvent>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.InteractionId == interactionId &&
                        item.IdempotencyKey == idempotencyKey,
                cancellationToken);

    private static void EnsureMatchingReplay(
        ContentInteractionEvent existing,
        RecordContentInteractionEventCommand request)
    {
        if (existing.MatchesReplay(
                request.Type,
                request.DurationSeconds,
                request.PositionSeconds,
                request.ProgressPercentage,
                request.Payload,
                request.OccurredAt))
        {
            return;
        }

        throw new RequestValidationException(
            "Idempotency key was already used for a different interaction event.");
    }

    private static void ApplyEvent(ContentInteraction interaction, ContentInteractionEvent item)
    {
        if (item.Type == ContentInteractionEventType.Opened)
        {
            interaction.Start();
            if (!interaction.IsCompleted)
            {
                interaction.Status = ProgressStatus.InProgress;
            }
        }

        if (item.DurationSeconds.HasValue)
        {
            interaction.AddTimeSpentSeconds(item.DurationSeconds.Value);
        }

        if (item.PositionSeconds.HasValue)
        {
            interaction.SetBookmark($"video:{item.PositionSeconds.Value.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        if (item.ProgressPercentage.HasValue)
        {
            interaction.UpdateProgress(item.ProgressPercentage.Value);
        }

        if (item.Type == ContentInteractionEventType.Completed)
        {
            interaction.Complete();
        }
        else
        {
            interaction.UpdateLastAccess();
        }
    }
}
