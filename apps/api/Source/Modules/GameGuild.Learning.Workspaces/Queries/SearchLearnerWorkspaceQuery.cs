using GameGuild.CQRS;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Program = GameGuild.Learning.Courses.Program;

namespace GameGuild.Learning.Workspaces;

public sealed record SearchLearnerWorkspaceQuery(Guid UserId, string Query, int Take = 20)
    : IQuery<IReadOnlyList<LearnerSearchResultDto>>;

public sealed class SearchLearnerWorkspaceQueryHandler(IApplicationDbContext context)
    : IQueryHandler<SearchLearnerWorkspaceQuery, IReadOnlyList<LearnerSearchResultDto>>
{
    public async Task<IReadOnlyList<LearnerSearchResultDto>> Handle(
        SearchLearnerWorkspaceQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Query.Trim().ToLowerInvariant();
        if (request.UserId == Guid.Empty || normalized.Length < 2)
        {
            return [];
        }

        var take = Math.Clamp(request.Take, 1, 50);
        var courseIds = await context.Set<ProgramEnrollment>()
            .AsNoTracking()
            .Where(enrollment =>
                enrollment.UserId == request.UserId &&
                enrollment.DeletedAt == null &&
                enrollment.EnrollmentStatus != GameGuild.Learning.Courses.EnrollmentStatus.Cancelled &&
                enrollment.EnrollmentStatus != GameGuild.Learning.Courses.EnrollmentStatus.Expired)
            .Select(enrollment => enrollment.ProgramId)
            .Distinct()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (courseIds.Length == 0)
        {
            return [];
        }

        var courses = await context.Set<Program>()
            .AsNoTracking()
            .Where(course =>
                courseIds.Contains(course.Id) &&
                course.DeletedAt == null &&
                (course.Title.ToLower().Contains(normalized) ||
                 (course.Description != null && course.Description.ToLower().Contains(normalized))))
            .OrderBy(course => course.Title)
            .Take(take)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var remaining = take - courses.Length;
        var content = remaining <= 0
            ? Array.Empty<ProgramContent>()
            : await context.Set<ProgramContent>()
                .AsNoTracking()
                .Where(item =>
                    courseIds.Contains(item.ProgramId) &&
                    item.DeletedAt == null &&
                    (item.Title.ToLower().Contains(normalized) ||
                     (item.Description != null && item.Description.ToLower().Contains(normalized)) ||
                     (item.Body != null && item.Body.ToLower().Contains(normalized))))
                .OrderBy(item => item.Title)
                .Take(remaining)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        var courseLookup = await context.Set<Program>()
            .AsNoTracking()
            .Where(course =>
                courseIds.Contains(course.Id) &&
                course.DeletedAt == null)
            .ToDictionaryAsync(course => course.Id, cancellationToken)
            .ConfigureAwait(false);

        var results = courses
            .Select(course => new LearnerSearchResultDto(
                course.Id,
                course.Id,
                course.Slug ?? course.Id.ToString(),
                "Course",
                course.Title,
                course.Description ?? string.Empty,
                $"/courses/{course.Slug ?? course.Id.ToString()}"))
            .Concat(content
                .Where(item => courseLookup.ContainsKey(item.ProgramId))
                .Select(item =>
                {
                    var course = courseLookup[item.ProgramId];
                    var slug = course.Slug ?? course.Id.ToString();
                    return new LearnerSearchResultDto(
                        item.Id,
                        item.ProgramId,
                        slug,
                        item.Type == ProgramContentType.Lesson ? "Lesson" : "Content",
                        item.Title,
                        item.Description ?? string.Empty,
                        item.Type == ProgramContentType.Lesson
                            ? $"/courses/{slug}/lessons/{item.Id}"
                            : $"/courses/{slug}/content");
                }))
            .Take(take)
            .ToArray();

        return results;
    }
}
