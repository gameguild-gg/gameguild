using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Cohorts;

public sealed record UpdateCohortScheduleItemRequest(
    string? Title,
    DateTime? StartsAt,
    DateTime? EndsAt,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    DateTime? DueAt,
    string? Location,
    string? MeetingUrl,
    CohortScheduleItemStatus Status,
    CohortVisibilityOverride VisibilityOverride);

public sealed record UpdateCohortScheduleItemCommand(
    Guid CourseId,
    Guid CohortId,
    Guid ItemId,
    int ExpectedVersion,
    UpdateCohortScheduleItemRequest Item) : ICommand<CohortScheduleDto>;

public sealed class UpdateCohortScheduleItemCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateCohortScheduleItemCommand, CohortScheduleDto>
{
    public async Task<CohortScheduleDto> Handle(
        UpdateCohortScheduleItemCommand request,
        CancellationToken cancellationToken)
    {
        var (schedule, items) = await CohortScheduleAggregate.LoadForUpdateAsync(
            context,
            request.CourseId,
            request.CohortId,
            request.ExpectedVersion,
            cancellationToken).ConfigureAwait(false);
        var item = items.SingleOrDefault(candidate => candidate.Id == request.ItemId)
            ?? throw new EntityNotFoundException(nameof(CohortScheduleItem), request.ItemId);

        item.UpdateDelivery(
            request.Item.Title,
            request.Item.StartsAt,
            request.Item.EndsAt,
            request.Item.AvailableFrom,
            request.Item.AvailableUntil,
            request.Item.DueAt,
            request.Item.Location,
            request.Item.MeetingUrl,
            request.Item.Status,
            request.Item.VisibilityOverride);
        schedule.Version++;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var unscheduledContentIds = await CohortScheduleReadModel.FindUnscheduledContentIdsAsync(
            context,
            request.CourseId,
            items,
            cancellationToken).ConfigureAwait(false);

        return CohortScheduleDtoMapper.ToDto(schedule, items, unscheduledContentIds);
    }
}

internal static class CohortScheduleAggregate
{
    internal static async Task<(CohortSchedule Schedule, CohortScheduleItem[] Items)> LoadForUpdateAsync(
        IApplicationDbContext context,
        Guid courseId,
        Guid cohortId,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        var ownsCohort = await context.Set<Cohort>()
            .AnyAsync(cohort => cohort.Id == cohortId && cohort.CourseId == courseId, cancellationToken)
            .ConfigureAwait(false);
        if (!ownsCohort)
        {
            throw new EntityNotFoundException(nameof(Cohort), cohortId);
        }

        var schedule = await context.Set<CohortSchedule>()
            .SingleOrDefaultAsync(candidate => candidate.CohortId == cohortId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(CohortSchedule), cohortId);
        if (schedule.Version != expectedVersion)
        {
            throw new CohortScheduleVersionConflictException(expectedVersion, schedule.Version);
        }

        var items = await context.Set<CohortScheduleItem>()
            .Where(item => item.CohortId == cohortId)
            .OrderBy(item => item.InstructionalWeek)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.StartsAt)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return (schedule, items);
    }
}
