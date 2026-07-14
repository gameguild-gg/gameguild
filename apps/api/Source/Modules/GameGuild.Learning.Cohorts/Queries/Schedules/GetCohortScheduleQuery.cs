using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Cohorts;

public sealed record GetCohortScheduleQuery(Guid CourseId, Guid CohortId)
    : IQuery<CohortScheduleDto?>;

public sealed class GetCohortScheduleQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetCohortScheduleQuery, CohortScheduleDto?>
{
    public async Task<CohortScheduleDto?> Handle(
        GetCohortScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var ownsCohort = await context.Set<Cohort>()
            .AsNoTracking()
            .AnyAsync(
                cohort => cohort.Id == request.CohortId && cohort.CourseId == request.CourseId,
                cancellationToken)
            .ConfigureAwait(false);
        if (!ownsCohort)
        {
            return null;
        }

        var schedule = await context.Set<CohortSchedule>()
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.CohortId == request.CohortId, cancellationToken)
            .ConfigureAwait(false);
        if (schedule is null)
        {
            return null;
        }

        var items = await context.Set<CohortScheduleItem>()
            .AsNoTracking()
            .Where(item => item.CohortId == request.CohortId)
            .OrderBy(item => item.InstructionalWeek)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.StartsAt)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var unscheduledContentIds = await CohortScheduleReadModel.FindUnscheduledContentIdsAsync(
            context,
            request.CourseId,
            items,
            cancellationToken).ConfigureAwait(false);

        return CohortScheduleDtoMapper.ToDto(schedule, items, unscheduledContentIds);
    }
}
