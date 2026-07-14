using GameGuild.CQRS;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Cohorts;

public sealed record ApplyCohortScheduleCommand(
    Guid CourseId,
    Guid CohortId,
    int ExpectedVersion,
    PreviewCohortScheduleRequest Rules,
    bool ConfirmAdvisories) : ICommand<CohortScheduleDto>;

public sealed class ApplyCohortScheduleCommandHandler(
    IApplicationDbContext context,
    CohortScheduleGenerator generator,
    ScheduleConflictDetector conflictDetector)
    : ICommandHandler<ApplyCohortScheduleCommand, CohortScheduleDto>
{
    public async Task<CohortScheduleDto> Handle(
        ApplyCohortScheduleCommand request,
        CancellationToken cancellationToken)
    {
        var preview = await CohortSchedulePreviewBuilder.BuildAsync(
            context,
            generator,
            conflictDetector,
            request.CourseId,
            request.CohortId,
            request.Rules,
            cancellationToken).ConfigureAwait(false);

        if (preview.HasBlockingConflicts)
        {
            throw new RequestValidationException("The schedule contains a blocking conflict.");
        }

        if (!request.ConfirmAdvisories &&
            preview.Conflicts.Any(conflict => conflict.Severity == ScheduleConflictSeverity.Advisory))
        {
            throw new RequestValidationException("The schedule contains advisories that require confirmation.");
        }

        var cohort = await context.Set<Cohort>()
            .SingleAsync(
                candidate => candidate.Id == request.CohortId && candidate.CourseId == request.CourseId,
                cancellationToken)
            .ConfigureAwait(false);
        var schedule = await context.Set<CohortSchedule>()
            .SingleOrDefaultAsync(
                candidate => candidate.CohortId == request.CohortId,
                cancellationToken)
            .ConfigureAwait(false);
        var currentVersion = schedule?.Version ?? 0;

        if (currentVersion != request.ExpectedVersion)
        {
            throw new CohortScheduleVersionConflictException(request.ExpectedVersion, currentVersion);
        }

        if (schedule is null)
        {
            schedule = CohortSchedule.Create(
                request.CohortId,
                request.Rules.TimezoneId,
                request.Rules.MeetingDays,
                request.Rules.MeetingStartTime,
                request.Rules.MeetingDurationMinutes,
                request.Rules.PacingMode,
                request.Rules.UnitsPerPeriod,
                request.Rules.ReleasePolicy,
                cohort.TenantId);
            context.Set<CohortSchedule>().Add(schedule);
        }
        else
        {
            schedule.UpdateRules(
                request.Rules.TimezoneId,
                request.Rules.MeetingDays,
                request.Rules.MeetingStartTime,
                request.Rules.MeetingDurationMinutes,
                request.Rules.PacingMode,
                request.Rules.UnitsPerPeriod,
                request.Rules.ReleasePolicy);

            var existingItems = await context.Set<CohortScheduleItem>()
                .Where(item => item.CohortId == request.CohortId)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            context.Set<CohortScheduleItem>().RemoveRange(existingItems);
        }

        schedule.Version = currentVersion + 1;
        var items = preview.Items.Select(item => CohortScheduleItem.Create(
                request.CohortId,
                item.ProgramContentId,
                item.AssessmentId,
                item.Type,
                item.Title,
                item.InstructionalWeek,
                item.SortOrder,
                item.StartsAt,
                item.EndsAt,
                item.AvailableFrom,
                item.AvailableUntil,
                item.DueAt,
                status: CohortScheduleItemStatus.Scheduled,
                tenantId: cohort.TenantId))
            .ToArray();

        context.Set<CohortScheduleItem>().AddRange(items);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var unscheduledContentIds = await CohortScheduleReadModel.FindUnscheduledContentIdsAsync(
            context,
            request.CourseId,
            items,
            cancellationToken).ConfigureAwait(false);

        return CohortScheduleDtoMapper.ToDto(schedule, items, unscheduledContentIds);
    }
}

public sealed class CohortScheduleVersionConflictException(int expectedVersion, int actualVersion)
    : Exception($"The schedule changed from version {expectedVersion} to {actualVersion}. Reload it before saving.")
{
    public int ExpectedVersion { get; } = expectedVersion;

    public int ActualVersion { get; } = actualVersion;
}

internal static class CohortScheduleReadModel
{
    internal static async Task<IReadOnlyList<Guid>> FindUnscheduledContentIdsAsync(
        IApplicationDbContext context,
        Guid courseId,
        IReadOnlyCollection<CohortScheduleItem> items,
        CancellationToken cancellationToken)
    {
        var scheduledContentIds = items
            .Where(item => item.ProgramContentId.HasValue)
            .Select(item => item.ProgramContentId!.Value)
            .ToHashSet();
        var canonicalContentIds = await context.Set<ProgramContent>()
            .AsNoTracking()
            .Where(item => item.ProgramId == courseId && item.DeletedAt == null)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return canonicalContentIds
            .Where(contentId => !scheduledContentIds.Contains(contentId))
            .ToArray();
    }
}
