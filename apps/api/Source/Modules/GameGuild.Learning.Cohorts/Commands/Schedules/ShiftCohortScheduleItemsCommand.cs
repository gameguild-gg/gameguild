using GameGuild.CQRS;

namespace GameGuild.Learning.Cohorts;

public sealed record ShiftCohortScheduleItemsCommand(
    Guid CourseId,
    Guid CohortId,
    Guid ItemId,
    int ExpectedVersion,
    int Days,
    ScheduleShiftScope Scope) : ICommand<CohortScheduleDto>;

public sealed class ShiftCohortScheduleItemsCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ShiftCohortScheduleItemsCommand, CohortScheduleDto>
{
    public async Task<CohortScheduleDto> Handle(
        ShiftCohortScheduleItemsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Days == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "The shift must be at least one day.");
        }

        var (schedule, items) = await CohortScheduleAggregate.LoadForUpdateAsync(
            context,
            request.CourseId,
            request.CohortId,
            request.ExpectedVersion,
            cancellationToken).ConfigureAwait(false);
        var anchor = items.SingleOrDefault(item => item.Id == request.ItemId)
            ?? throw new EntityNotFoundException(nameof(CohortScheduleItem), request.ItemId);
        var selected = request.Scope == ScheduleShiftScope.Single
            ? [anchor]
            : items.Where(item =>
                    item.InstructionalWeek > anchor.InstructionalWeek ||
                    (item.InstructionalWeek == anchor.InstructionalWeek && item.SortOrder >= anchor.SortOrder))
                .ToArray();
        var offset = TimeSpan.FromDays(request.Days);

        foreach (var item in selected)
        {
            item.Shift(offset);
        }

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
