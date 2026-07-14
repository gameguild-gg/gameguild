using GameGuild.CQRS;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Cohorts;

public sealed record PreviewCohortScheduleQuery(
    Guid CourseId,
    Guid CohortId,
    PreviewCohortScheduleRequest Request) : IQuery<CohortSchedulePreviewDto>;

public sealed class PreviewCohortScheduleQueryHandler(
    IApplicationDbContext context,
    CohortScheduleGenerator generator,
    ScheduleConflictDetector conflictDetector)
    : IQueryHandler<PreviewCohortScheduleQuery, CohortSchedulePreviewDto>
{
    public async Task<CohortSchedulePreviewDto> Handle(
        PreviewCohortScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var preview = await CohortSchedulePreviewBuilder.BuildAsync(
            context,
            generator,
            conflictDetector,
            request.CourseId,
            request.CohortId,
            request.Request,
            cancellationToken).ConfigureAwait(false);

        return preview.ToDto();
    }
}

internal static class CohortSchedulePreviewBuilder
{
    internal static async Task<CohortSchedulePreview> BuildAsync(
        IApplicationDbContext context,
        CohortScheduleGenerator generator,
        ScheduleConflictDetector conflictDetector,
        Guid courseId,
        Guid cohortId,
        PreviewCohortScheduleRequest rules,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var cohort = await context.Set<Cohort>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == cohortId && candidate.CourseId == courseId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Cohort), cohortId);

        var content = await context.Set<ProgramContent>()
            .AsNoTracking()
            .Where(item => item.ProgramId == courseId && item.DeletedAt == null)
            .OrderBy(item => item.SortOrder)
            .Select(item => new CanonicalScheduleContent(
                item.Id,
                null,
                item.ParentId,
                item.Title,
                item.Type,
                item.SortOrder,
                item.EstimatedMinutes))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var generated = generator.Generate(new CohortScheduleGenerationRequest(
            cohortId,
            rules.FirstInstructionalDate,
            rules.CohortEndDate,
            rules.TimezoneId,
            rules.MeetingDays,
            rules.MeetingStartTime,
            rules.MeetingDurationMinutes,
            rules.PacingMode,
            rules.UnitsPerPeriod,
            rules.ReleasePolicy,
            rules.SkippedDates,
            content,
            rules.AssessmentDueOffsetDays));

        if (cohort.InstructorId is not Guid instructorId)
        {
            return generated;
        }

        var otherCohortIds = await context.Set<Cohort>()
            .AsNoTracking()
            .Where(candidate => candidate.InstructorId == instructorId && candidate.Id != cohortId)
            .Select(candidate => candidate.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (otherCohortIds.Length == 0)
        {
            return generated;
        }

        var instructorSchedule = await context.Set<CohortScheduleItem>()
            .AsNoTracking()
            .Where(item =>
                otherCohortIds.Contains(item.CohortId) &&
                item.Type == CohortScheduleItemType.LiveSession &&
                item.Status != CohortScheduleItemStatus.Cancelled &&
                item.StartsAt.HasValue &&
                item.EndsAt.HasValue)
            .Select(item => new InstructorScheduleSlot(
                item.CohortId,
                item.StartsAt!.Value,
                item.EndsAt!.Value))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var externalConflicts = conflictDetector.Detect(
            cohortId,
            rules.CohortEndDate,
            generated.Items,
            instructorSchedule);
        var conflicts = generated.Conflicts
            .Concat(externalConflicts)
            .Distinct()
            .ToArray();

        return generated with { Conflicts = conflicts };
    }
}
