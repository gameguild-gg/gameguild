using GameGuild.Identity.Authorization;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Course manager resolution for grading notifications. Same permission-name scheme as
/// AssessmentsController.CanManageCourseAsync: Program.{courseId}.{Edit|Create|Delete}.
/// </summary>
internal static class CourseManagers
{
    // ponytail: loads ALL active direct grants then filters in memory (O(grants) per submit);
    // push the array-containment into SQL if notification volume ever matters. Tenant/global
    // DEFAULT grants (UserId == null) are deliberately not enumerated — defaults carry generic
    // operations permissions, not per-course resource grants, and fanning out to every tenant
    // member would spam.
    public static async Task<IReadOnlyList<Guid>> GetManagerUserIdsAsync(IApplicationDbContext context, Guid courseId)
    {
        var creatorId = await context.Set<Program>()
            .Where(p => p.Id == courseId)
            .Select(p => p.CreatorId)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        var permissionNames = new[]
        {
            $"{nameof(Program)}.{courseId}.{PermissionType.Edit}",
            $"{nameof(Program)}.{courseId}.{PermissionType.Create}",
            $"{nameof(Program)}.{courseId}.{PermissionType.Delete}"
        };

        var grants = await context.Set<TenantPermission>()
            .Where(tp => tp.UserId != null && tp.IsActive && tp.DeletedAt == null)
            .ToListAsync().ConfigureAwait(false);

        var managers = grants
            .Where(tp => !tp.IsExpired() &&
                         permissionNames.Any(tp.HasEffectivePermission))
            .Select(tp => tp.UserId!.Value)
            .ToList();

        if (creatorId is { } creator)
        {
            managers.Add(creator);
        }

        return managers.Distinct().ToList();
    }
}
