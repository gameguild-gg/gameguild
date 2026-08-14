using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Projects;
using GameGuild.Teams;

namespace GameGuild.Assets.Security;

public sealed class ProjectAssetParentAuthorizationResolver(
    IProjectAuthorizationService projectAuthorizationService,
    IActorContextAccessor actorContextAccessor) : IAssetParentAuthorizationResolver
{
    public bool Supports(string resourceType) =>
        string.Equals(resourceType, nameof(Project), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(resourceType, "projects", StringComparison.OrdinalIgnoreCase);

    public Task<bool> CanReadAsync(
        Guid parentResourceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (actorContextAccessor.ActorContext.SubjectIdAsGuid != userId)
            return Task.FromResult(false);

        return projectAuthorizationService.HasPermissionAsync(
            parentResourceId,
            PermissionType.Read,
            cancellationToken);
    }

    public Task<bool> CanManageAsync(
        Guid parentResourceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (actorContextAccessor.ActorContext.SubjectIdAsGuid != userId)
            return Task.FromResult(false);
        return projectAuthorizationService.HasPermissionAsync(parentResourceId, PermissionType.Edit, cancellationToken);
    }
}

public sealed class TeamAssetParentAuthorizationResolver(
    ITeamAuthorizationService teamAuthorizationService,
    IActorContextAccessor actorContextAccessor) : IAssetParentAuthorizationResolver
{
    public bool Supports(string resourceType) =>
        string.Equals(resourceType, nameof(Team), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(resourceType, "teams", StringComparison.OrdinalIgnoreCase);

    public Task<bool> CanReadAsync(
        Guid parentResourceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (actorContextAccessor.ActorContext.SubjectIdAsGuid != userId)
            return Task.FromResult(false);

        return teamAuthorizationService.HasAuthorityAsync(
            parentResourceId,
            TeamMemberAuthority.Viewer,
            cancellationToken);
    }

    public Task<bool> CanManageAsync(
        Guid parentResourceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (actorContextAccessor.ActorContext.SubjectIdAsGuid != userId)
            return Task.FromResult(false);
        return teamAuthorizationService.HasAuthorityAsync(parentResourceId, TeamMemberAuthority.Manager, cancellationToken);
    }
}
