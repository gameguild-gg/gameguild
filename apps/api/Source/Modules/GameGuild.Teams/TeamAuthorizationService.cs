using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Teams;

public interface ITeamAuthorizationService
{
    Task<bool> CanCreateAsync(CancellationToken cancellationToken = default);
    Task<bool> HasAuthorityAsync(Guid teamId, TeamMemberAuthority required, CancellationToken cancellationToken = default);
    IQueryable<Team> ApplyMembershipAccess(IQueryable<Team> query);
}

public sealed class TeamAuthorizationService(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor) : ITeamAuthorizationService
{
    public async Task<bool> CanCreateAsync(CancellationToken cancellationToken = default)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not { } userId || actor.TenantId is not { } tenantId)
            return false;
        return await IsActiveUserAsync(userId, cancellationToken).ConfigureAwait(false) &&
               await context.Set<TenantMember>().AsNoTracking().AnyAsync(member =>
                   member.UserId == userId &&
                   member.TenantId == tenantId &&
                   member.IsActive &&
                   member.DeletedAt == null,
                   cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasAuthorityAsync(
        Guid teamId,
        TeamMemberAuthority required,
        CancellationToken cancellationToken = default)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not { } userId)
            return false;

        var team = await context.Set<Team>().AsNoTracking()
            .Where(candidate => candidate.Id == teamId && candidate.IsActive && candidate.DeletedAt == null)
            .Select(candidate => new { candidate.TenantId, candidate.Visibility })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (team == null) return false;
        if (actor.TenantId == null || team.TenantId != actor.TenantId) return false;
        if (!await IsActiveUserAsync(userId, cancellationToken).ConfigureAwait(false)) return false;
        var activeTenantMember = await context.Set<TenantMember>().AsNoTracking().AnyAsync(member =>
            member.UserId == userId &&
            member.TenantId == actor.TenantId &&
            member.IsActive &&
            member.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (!activeTenantMember) return false;
        if (actor.IsSystemAdmin || actor.IsTenantAdmin) return true;

        return await context.Set<TeamMember>().AsNoTracking().AnyAsync(member =>
            member.TeamId == teamId &&
            member.UserId == userId &&
            member.Authority >= required &&
            member.IsActive &&
            member.LeftAt == null &&
            member.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
    }

    public IQueryable<Team> ApplyMembershipAccess(IQueryable<Team> query)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not { } userId)
            return query.Where(team => team.Visibility == TeamVisibility.Public && team.IsActive);
        if (actor.TenantId is not { } tenantId)
            return query.Where(team => team.Visibility == TeamVisibility.Public && team.IsActive);
        if (actor.IsSystemAdmin || actor.IsTenantAdmin)
            return query.Where(team =>
                team.TenantId == tenantId &&
                team.IsActive &&
                team.DeletedAt == null &&
                context.Set<TenantMember>().Any(member =>
                    member.UserId == userId &&
                    member.TenantId == tenantId &&
                    member.IsActive &&
                    member.DeletedAt == null));

        return query.Where(team =>
            team.TenantId == tenantId &&
            team.IsActive &&
            team.DeletedAt == null &&
            context.Set<TeamMember>().Any(member =>
                member.TeamId == team.Id &&
                member.UserId == userId &&
                member.IsActive &&
                member.LeftAt == null &&
                member.DeletedAt == null));
    }

    private Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Set<User>().AsNoTracking().AnyAsync(user =>
            user.Id == userId &&
            user.IsActive &&
            !user.IsSuspended &&
            user.DeletedAt == null,
            cancellationToken);
}
