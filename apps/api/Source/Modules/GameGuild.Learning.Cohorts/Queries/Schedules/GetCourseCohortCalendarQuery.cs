using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Cohorts;

public sealed record GetCourseCohortCalendarQuery(
    Guid CourseId,
    Guid? CohortId = null,
    DateTime? From = null,
    DateTime? To = null) : IQuery<CourseCohortCalendarDto>;

public sealed class GetCourseCohortCalendarQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetCourseCohortCalendarQuery, CourseCohortCalendarDto>
{
    public async Task<CourseCohortCalendarDto> Handle(
        GetCourseCohortCalendarQuery request,
        CancellationToken cancellationToken)
    {
        if (request.From.HasValue && request.To.HasValue && request.To < request.From)
        {
            throw new ArgumentException("The calendar end must not precede its start.", nameof(request));
        }

        var cohortQuery = context.Set<Cohort>()
            .AsNoTracking()
            .Where(cohort => cohort.CourseId == request.CourseId);
        if (request.CohortId.HasValue)
        {
            cohortQuery = cohortQuery.Where(cohort => cohort.Id == request.CohortId.Value);
        }

        var cohorts = await cohortQuery
            .Select(cohort => new { cohort.Id, cohort.Name })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (cohorts.Length == 0)
        {
            return new CourseCohortCalendarDto(request.CourseId, []);
        }

        var cohortIds = cohorts.Select(cohort => cohort.Id).ToArray();
        var itemQuery = context.Set<CohortScheduleItem>()
            .AsNoTracking()
            .Where(item => cohortIds.Contains(item.CohortId));
        if (request.From.HasValue)
        {
            itemQuery = itemQuery.Where(item =>
                (item.StartsAt ?? item.AvailableFrom ?? item.DueAt) >= request.From.Value);
        }
        if (request.To.HasValue)
        {
            itemQuery = itemQuery.Where(item =>
                (item.StartsAt ?? item.AvailableFrom ?? item.DueAt) <= request.To.Value);
        }

        var items = await itemQuery
            .OrderBy(item => item.StartsAt ?? item.AvailableFrom ?? item.DueAt)
            .ThenBy(item => item.SortOrder)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var cohortNames = cohorts.ToDictionary(cohort => cohort.Id, cohort => cohort.Name);
        var entries = items.Select(item => new CohortCalendarEntryDto(
                item.CohortId,
                cohortNames[item.CohortId],
                item.Id,
                item.Type,
                item.Title ?? string.Empty,
                item.StartsAt,
                item.EndsAt,
                item.AvailableFrom,
                item.DueAt,
                item.Status))
            .ToArray();

        return new CourseCohortCalendarDto(request.CourseId, entries);
    }
}
