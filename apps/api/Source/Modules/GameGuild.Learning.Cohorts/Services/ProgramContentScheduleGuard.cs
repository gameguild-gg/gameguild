using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Cohorts;

public sealed class ProgramContentScheduleGuard(IApplicationDbContext context)
    : IProgramContentScheduleGuard
{
    public Task<bool> HasActiveScheduleReference(
        Guid contentId,
        CancellationToken cancellationToken = default) =>
        (from item in context.Set<CohortScheduleItem>().AsNoTracking()
         join cohort in context.Set<Cohort>().AsNoTracking()
             on item.CohortId equals cohort.Id
         where item.ProgramContentId == contentId &&
               item.Status != CohortScheduleItemStatus.Cancelled &&
               cohort.Status != CohortStatus.Cancelled
         select item.Id)
        .AnyAsync(cancellationToken);
}
