using GameGuild.Projects;
using GameGuild.Teams;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.ProjectWork;

public static class ProjectWorkAssignmentPolicy
{
    public static Task<bool> IsEligibleAsync(
        IApplicationDbContext context,
        Guid projectId,
        Guid userId,
        DateTime at,
        CancellationToken cancellationToken = default) =>
        (from allocation in context.Set<ProjectMemberAllocation>().AsNoTracking()
         join projectTeam in context.Set<ProjectTeam>().AsNoTracking()
             on allocation.ProjectTeamId equals projectTeam.Id
         join member in context.Set<TeamMember>().AsNoTracking()
             on new { projectTeam.TeamId, allocation.UserId }
             equals new { member.TeamId, member.UserId }
         where allocation.ProjectId == projectId &&
               allocation.UserId == userId &&
               allocation.IsActive &&
               allocation.DeletedAt == null &&
               allocation.StartsAt <= at &&
               (!allocation.EndsAt.HasValue || allocation.EndsAt > at) &&
               projectTeam.ProjectId == projectId &&
               projectTeam.IsActive &&
               projectTeam.DeletedAt == null &&
               projectTeam.EndedAt == null &&
               member.IsActive &&
               member.DeletedAt == null &&
               member.LeftAt == null
         select allocation.Id).AnyAsync(cancellationToken);
}
