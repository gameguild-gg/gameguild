using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using ResourceTenantId = GameGuild.CQRS.Models.TenantId;

namespace GameGuild.Projects;

public interface IProjectAuthorizationService
{
    Task<bool> IsActorActiveTenantMemberAsync(CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(Guid projectId, PermissionType permission, CancellationToken cancellationToken = default);

    IQueryable<Project> ApplyReadAccess(IQueryable<Project> query) => query;
}

public sealed class ProjectAuthorizationService(IApplicationDbContext context, IActorContextAccessor actorContextAccessor)
    : IProjectAuthorizationService
{
    public async Task<bool> HasPermissionAsync(Guid projectId, PermissionType permission, CancellationToken cancellationToken = default)
    {
        var actor = actorContextAccessor.ActorContext;
        var project = await context.Set<Project>()
            .Where(project =>
                project.Id == projectId &&
                project.DeletedAt == null)
            .Select(project => new
            {
                project.CreatedById,
                project.Status,
                project.TenantId,
                project.Visibility
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (project == null)
            return false;

        if (permission == PermissionType.Read &&
            project.Visibility == ContentVisibility.Public &&
            project.Status == ContentStatus.Published)
            return true;

        if (!await IsActorActiveTenantMemberAsync(cancellationToken).ConfigureAwait(false))
            return false;

        var actorId = actor.SubjectIdAsGuid!.Value;
        if (actor.IsSystemAdmin || actor.IsTenantAdmin)
            return actor.IsSystemAdmin || project.TenantId == actor.TenantId;
        if (project.TenantId != actor.TenantId)
            return false;
        if (project.CreatedById == actorId)
            return true;

        var collaborators = await context.Set<ProjectCollaborator>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.ProjectId == projectId &&
                candidate.UserId == actorId &&
                candidate.IsActive &&
                candidate.DeletedAt == null &&
                candidate.LeftAt == null)
            .Select(candidate => new { candidate.Role, candidate.Permissions })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (collaborators.Any(candidate =>
                string.Equals(candidate.Role, ProjectRoles.Owner, StringComparison.OrdinalIgnoreCase) ||
                HasExactPermission(candidate.Permissions, permission)))
            return true;

        var projectTeams = await context.Set<ProjectTeam>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.ProjectId == projectId &&
                candidate.IsActive &&
                candidate.DeletedAt == null &&
                candidate.EndedAt == null)
            .Select(candidate => new { candidate.TeamId, candidate.Role, candidate.Permissions })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var projectTeam in projectTeams)
        {
            if (!string.Equals(projectTeam.Role, ProjectRoles.Owner, StringComparison.OrdinalIgnoreCase) &&
                !HasExactPermission(projectTeam.Permissions, permission))
                continue;

            var activeMember = await context.Set<TeamMember>()
                .AsNoTracking()
                .AnyAsync(member =>
                    member.TeamId == projectTeam.TeamId &&
                    member.UserId == actorId &&
                    member.IsActive &&
                    member.DeletedAt == null &&
                    context.Set<Team>().Any(team =>
                        team.Id == member.TeamId &&
                        team.IsActive &&
                        team.DeletedAt == null),
                    cancellationToken)
                .ConfigureAwait(false);
            if (activeMember)
                return true;
        }

        var resourceTenantId = new ResourceTenantId(project.TenantId!.Value);
        var permissionName = permission.ToString();
        return await context.Set<ResourceUserPermission>()
            .AsNoTracking()
            .AnyAsync(candidate =>
                candidate.TenantId == resourceTenantId &&
                candidate.UserId == actorId &&
                (candidate.ResourceType == nameof(Project) || candidate.ResourceType == "projects") &&
                candidate.ResourceId == projectId.ToString() &&
                candidate.RevokedAt == null &&
                (!candidate.ExpiresAt.HasValue || candidate.ExpiresAt > SystemClock.UtcNow) &&
                candidate.Permissions.Contains(permissionName),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public IQueryable<Project> ApplyReadAccess(IQueryable<Project> query)
    {
        var actor = actorContextAccessor.ActorContext;
        var publicProjects = query.Where(project =>
            project.Visibility == ContentVisibility.Public &&
            project.Status == ContentStatus.Published);

        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid == null || actor.TenantId == null)
            return publicProjects;
        var actorId = actor.SubjectIdAsGuid.Value;
        var tenantId = actor.TenantId.Value;
        var isActiveUser = context.Set<User>().Any(user =>
            user.Id == actorId &&
            user.IsActive &&
            !user.IsSuspended &&
            user.DeletedAt == null);
        if (actor.IsSystemAdmin)
            return query.Where(project =>
                (project.Visibility == ContentVisibility.Public && project.Status == ContentStatus.Published) ||
                isActiveUser);

        if (actor.IsTenantAdmin)
            return query.Where(project =>
                (project.Visibility == ContentVisibility.Public && project.Status == ContentStatus.Published) ||
                (project.TenantId == tenantId &&
                 isActiveUser &&
                 context.Set<TenantMember>().Any(member =>
                     member.UserId == actorId &&
                     member.TenantId == tenantId &&
                     member.IsActive &&
                     member.DeletedAt == null)));

        var resourceTenantId = new ResourceTenantId(tenantId);
        return query.Where(project =>
            (project.Visibility == ContentVisibility.Public && project.Status == ContentStatus.Published) ||
            (project.TenantId == tenantId &&
             context.Set<User>().Any(user =>
                 user.Id == actorId &&
                 user.IsActive &&
                 !user.IsSuspended &&
                 user.DeletedAt == null) &&
             context.Set<TenantMember>().Any(member =>
                 member.UserId == actorId &&
                 member.TenantId == tenantId &&
                 member.IsActive &&
                 member.DeletedAt == null) &&
             (project.CreatedById == actorId ||
              project.Collaborators.Any(collaborator =>
                  collaborator.UserId == actorId &&
                  collaborator.IsActive &&
                  collaborator.DeletedAt == null &&
                  collaborator.LeftAt == null &&
                  (collaborator.Role == ProjectRoles.Owner ||
                   ("," + collaborator.Permissions.Replace(" ", "").Replace(";", ",").Replace("|", ",").ToUpper() + ",")
                       .Contains(",READ,"))) ||
              context.Set<ProjectTeam>().Any(projectTeam =>
                  projectTeam.ProjectId == project.Id &&
                  projectTeam.IsActive &&
                  projectTeam.DeletedAt == null &&
                  projectTeam.EndedAt == null &&
                  (projectTeam.Role == ProjectRoles.Owner ||
                   ("," + (projectTeam.Permissions ?? string.Empty).Replace(" ", "").Replace(";", ",").Replace("|", ",").ToUpper() + ",")
                       .Contains(",READ,")) &&
                  context.Set<Team>().Any(team =>
                      team.Id == projectTeam.TeamId &&
                      team.IsActive &&
                      team.DeletedAt == null) &&
                  context.Set<TeamMember>().Any(member =>
                      member.TeamId == projectTeam.TeamId &&
                      member.UserId == actorId &&
                      member.IsActive &&
                      member.DeletedAt == null)) ||
              context.Set<ResourceUserPermission>().Any(grant =>
                  grant.TenantId == resourceTenantId &&
                  grant.UserId == actorId &&
                  (grant.ResourceType == nameof(Project) || grant.ResourceType == "projects") &&
                  grant.ResourceId == project.Id.ToString() &&
                  grant.RevokedAt == null &&
                  (!grant.ExpiresAt.HasValue || grant.ExpiresAt > SystemClock.UtcNow) &&
                  grant.Permissions.Contains(nameof(PermissionType.Read))))));
    }

    public async Task<bool> IsActorActiveTenantMemberAsync(CancellationToken cancellationToken = default)
    {
        var actor = actorContextAccessor.ActorContext;
        var actorId = actor.SubjectIdAsGuid;
        if (!actor.IsAuthenticated || actorId == null || actor.TenantId == null)
            return false;

        var activeUser = await context.Set<User>()
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == actorId.Value &&
                user.IsActive &&
                !user.IsSuspended &&
                user.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (!activeUser)
            return false;

        if (actor.IsSystemAdmin)
            return true;

        return await context.Set<TenantMember>()
            .AsNoTracking()
            .AnyAsync(member =>
                member.UserId == actorId.Value &&
                member.TenantId == actor.TenantId.Value &&
                member.IsActive &&
                member.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool HasExactPermission(string? permissions, PermissionType permission)
    {
        if (string.IsNullOrWhiteSpace(permissions))
            return false;

        return permissions
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(permission.ToString(), StringComparer.OrdinalIgnoreCase);
    }
}
