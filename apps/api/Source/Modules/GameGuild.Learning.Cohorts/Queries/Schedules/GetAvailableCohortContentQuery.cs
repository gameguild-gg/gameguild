using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Enrollments;
using Microsoft.EntityFrameworkCore;
using LearningEnrollmentStatus = GameGuild.Learning.Enrollments.EnrollmentStatus;

namespace GameGuild.Learning.Cohorts;

public sealed record GetAvailableCohortContentQuery(Guid CourseId, Guid CohortId)
    : IQuery<IReadOnlyList<AvailableCohortContentDto>>;

public sealed record AvailableCohortContentDto(
    Guid ContentId,
    Guid? ParentId,
    string Title,
    string? Description,
    string? Body,
    ProgramContentType Type,
    int SortOrder,
    int InstructionalWeek,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    DateTime? DueAt);

public sealed class GetAvailableCohortContentQueryHandler(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetAvailableCohortContentQuery, IReadOnlyList<AvailableCohortContentDto>>
{
    public async Task<IReadOnlyList<AvailableCohortContentDto>> Handle(
        GetAvailableCohortContentQuery request,
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not Guid userId)
        {
            return [];
        }

        var cohort = await context.Set<Cohort>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == request.CohortId &&
                candidate.CourseId == request.CourseId &&
                candidate.DeletedAt == null)
            .Select(candidate => new { candidate.TenantId })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (cohort is null || cohort.TenantId != actor.TenantId)
        {
            return [];
        }

        var isEnrolled = await context.Set<Enrollment>()
            .AsNoTracking()
            .AnyAsync(
                enrollment =>
                    enrollment.CourseId == request.CourseId &&
                    enrollment.CohortId == request.CohortId &&
                    enrollment.UserId == userId &&
                    enrollment.DeletedAt == null &&
                    (enrollment.TenantId == null || enrollment.TenantId == cohort.TenantId) &&
                    (enrollment.Status == LearningEnrollmentStatus.Active ||
                     enrollment.Status == LearningEnrollmentStatus.Completed),
                cancellationToken)
            .ConfigureAwait(false);
        if (!isEnrolled)
        {
            return [];
        }

        var now = SystemClock.UtcNow;
        return await (
                from item in context.Set<CohortScheduleItem>().AsNoTracking()
                join content in context.Set<ProgramContent>().AsNoTracking()
                    on item.ProgramContentId equals (Guid?)content.Id
                where item.CohortId == request.CohortId
                      && item.Type == CohortScheduleItemType.ContentRelease
                      && item.DeletedAt == null
                      && item.VisibilityOverride != CohortVisibilityOverride.Hidden
                      && (item.Status == CohortScheduleItemStatus.Scheduled
                          || item.Status == CohortScheduleItemStatus.Published
                          || item.Status == CohortScheduleItemStatus.Completed)
                      && (!item.AvailableFrom.HasValue || item.AvailableFrom.Value <= now)
                      && (!item.AvailableUntil.HasValue || item.AvailableUntil.Value >= now)
                      && (item.TenantId == null || item.TenantId == cohort.TenantId)
                      && content.ProgramId == request.CourseId
                      && content.DeletedAt == null
                      && (content.TenantId == null || content.TenantId == cohort.TenantId)
                      && (item.VisibilityOverride == CohortVisibilityOverride.Visible
                          || (item.VisibilityOverride == CohortVisibilityOverride.Inherited
                              && (content.Visibility == Visibility.Public
                                  || content.Visibility == Visibility.Internal)))
                orderby item.InstructionalWeek, item.SortOrder, content.SortOrder
                select new AvailableCohortContentDto(
                    content.Id,
                    content.ParentId,
                    content.Title,
                    content.Description,
                    content.Body,
                    content.Type,
                    content.SortOrder,
                    item.InstructionalWeek,
                    item.AvailableFrom,
                    item.AvailableUntil,
                    item.DueAt))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
