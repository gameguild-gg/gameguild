using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using GameGuild.ProjectWork;
using GameGuild.Teams;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.Dashboard;

public sealed record DashboardWorkspaceCounts(
    int Teams,
    int Projects,
    int PendingTasks,
    int Invitations);

public sealed record DashboardWorkspaceContextData(
    IReadOnlyList<DashboardContextSummary> Contexts,
    DashboardWorkspaceCounts Counts);

public interface IDashboardWorkspaceContextService
{
    Task<DashboardWorkspaceContextData> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class DashboardWorkspaceContextService(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor,
    ITeamAuthorizationService teamAuthorizationService,
    IProjectAuthorizationService projectAuthorizationService) : IDashboardWorkspaceContextService
{
    public async Task<DashboardWorkspaceContextData> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid != userId)
            return new DashboardWorkspaceContextData([], new DashboardWorkspaceCounts(0, 0, 0, 0));

        var teamQuery = teamAuthorizationService.ApplyMembershipAccess(context.Set<Team>().AsNoTracking());
        var projectQuery = projectAuthorizationService.ApplyWorkspaceAccess(context.Set<Project>().AsNoTracking());

        var teams = await teamQuery.OrderByDescending(team => team.UpdatedAt).Take(8)
            .Select(team => new DashboardContextSummary(
                DashboardContextTypes.Team,
                team.Id,
                team.Name,
                $"/dashboard/teams/{team.Slug}"))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var projects = await projectQuery.OrderByDescending(project => project.UpdatedAt).Take(8)
            .Select(project => new DashboardContextSummary(
                DashboardContextTypes.Project,
                project.Id,
                project.Title,
                $"/dashboard/projects/{project.Slug}"))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var teamCount = await teamQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var projectCount = await projectQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var actorEmail = await context.Set<User>().AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive && user.DeletedAt == null)
            .Select(user => user.Email)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var normalizedEmail = actorEmail == null ? null : actorEmail.Trim().ToLower();
        var invitations = await context.Set<TeamInvitation>().AsNoTracking().CountAsync(invitation =>
            invitation.TenantId == actor.TenantId &&
            (invitation.InvitedUserId == userId ||
             (invitation.InvitedUserId == null && normalizedEmail != null &&
              invitation.InvitedEmail != null && invitation.InvitedEmail.ToLower() == normalizedEmail)) &&
            invitation.UsedAt == null &&
            invitation.RevokedAt == null &&
            invitation.ExpiresAt > SystemClock.UtcNow &&
            invitation.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        var pendingTasks = await context.Set<ProjectWorkTask>().AsNoTracking().CountAsync(task =>
            task.AssigneeUserId == userId &&
            task.Status != ProjectWorkTaskStatus.Done &&
            task.Status != ProjectWorkTaskStatus.Cancelled &&
            task.DeletedAt == null &&
            projectQuery.Any(project => project.Id == task.ProjectId),
            cancellationToken).ConfigureAwait(false);

        return new DashboardWorkspaceContextData(
            teams.Concat(projects).ToArray(),
            new DashboardWorkspaceCounts(teamCount, projectCount, pendingTasks, invitations));
    }
}
