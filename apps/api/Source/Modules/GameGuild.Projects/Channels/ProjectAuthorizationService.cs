using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Projects;

public interface IProjectAuthorizationService
{
    Task<bool> IsActorActiveTenantMemberAsync(CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(Guid projectId, PermissionType permission, CancellationToken cancellationToken = default);
}

public sealed class ProjectAuthorizationService(IApplicationDbContext context, IActorContextAccessor actorContextAccessor)
    : IProjectAuthorizationService
{
    public async Task<bool> HasPermissionAsync(Guid projectId, PermissionType permission, CancellationToken cancellationToken = default)
    {
        if (!await IsActorActiveTenantMemberAsync(cancellationToken).ConfigureAwait(false))
            return false;

        var actor = actorContextAccessor.ActorContext;
        var actorId = actor.SubjectIdAsGuid!.Value;

        var project = await context.Set<Project>()
            .Where(project =>
                project.Id == projectId &&
                project.DeletedAt == null &&
                project.TenantId == actor.TenantId)
            .Select(project => new { project.CreatedById })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (project == null)
            return false;
        if (actor.IsSystemAdmin || actor.IsTenantAdmin)
            return true;
        if (project.CreatedById == actorId)
            return true;

        var collaborator = await context.Set<ProjectCollaborator>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.ProjectId == projectId &&
                candidate.UserId == actorId &&
                candidate.IsActive &&
                candidate.DeletedAt == null)
            .Select(candidate => new { candidate.Role, candidate.Permissions })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (collaborator == null)
            return false;
        if (string.Equals(collaborator.Role, ProjectRoles.Owner, StringComparison.OrdinalIgnoreCase))
            return true;

        return collaborator.Permissions
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(permission.ToString(), StringComparer.OrdinalIgnoreCase);
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
}
