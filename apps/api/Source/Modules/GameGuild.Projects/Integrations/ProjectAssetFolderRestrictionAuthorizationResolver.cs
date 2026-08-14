using GameGuild.Assets;
using GameGuild.Teams;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Projects.Integrations;

public sealed class ProjectAssetFolderRestrictionAuthorizationResolver(IApplicationDbContext context)
    : IAssetFolderRestrictionAuthorizationResolver
{
    public bool Supports(AssetFolderRestrictionMode restrictionMode, string parentResourceType) =>
        restrictionMode is AssetFolderRestrictionMode.SelectedTeams or
            AssetFolderRestrictionMode.TeamAuthorities or
            AssetFolderRestrictionMode.AllocatedProjectMembers;

    public Task<bool> IsAuthorizedAsync(
        AssetFolder folder,
        Guid userId,
        CancellationToken cancellationToken = default) => folder.RestrictionMode switch
        {
            AssetFolderRestrictionMode.SelectedTeams => IsMemberOfSelectedTeamAsync(folder, userId, cancellationToken),
            AssetFolderRestrictionMode.TeamAuthorities => HasSelectedAuthorityAsync(folder, userId, cancellationToken),
            AssetFolderRestrictionMode.AllocatedProjectMembers => IsAllocatedProjectMemberAsync(folder, userId, cancellationToken),
            _ => Task.FromResult(false)
        };

    private Task<bool> IsMemberOfSelectedTeamAsync(
        AssetFolder folder,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var teamIds = folder.AllowedTeamIds.ToArray();
        if (teamIds.Length == 0) return Task.FromResult(false);

        return (from member in context.Set<TeamMember>().AsNoTracking()
                join team in context.Set<Team>().AsNoTracking() on member.TeamId equals team.Id
                where teamIds.Contains(member.TeamId) && member.UserId == userId && member.IsActive &&
                      member.LeftAt == null && member.DeletedAt == null && team.TenantId == folder.TenantId &&
                      team.IsActive && team.DeletedAt == null
                select member.Id).AnyAsync(cancellationToken);
    }

    private Task<bool> HasSelectedAuthorityAsync(
        AssetFolder folder,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!folder.BelongsTo("Team", folder.ParentResourceId)) return Task.FromResult(false);

        var authorities = folder.AllowedAuthorities
            .Select(value => Enum.TryParse<TeamMemberAuthority>(value, true, out var authority)
                ? authority
                : (TeamMemberAuthority?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
        if (authorities.Length == 0) return Task.FromResult(false);

        return context.Set<TeamMember>().AsNoTracking().AnyAsync(member =>
            member.TeamId == folder.ParentResourceId && member.UserId == userId &&
            authorities.Contains(member.Authority) && member.IsActive && member.LeftAt == null &&
            member.DeletedAt == null,
            cancellationToken);
    }

    private Task<bool> IsAllocatedProjectMemberAsync(
        AssetFolder folder,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!folder.BelongsTo("Project", folder.ParentResourceId)) return Task.FromResult(false);
        var now = SystemClock.UtcNow;

        return (from allocation in context.Set<ProjectMemberAllocation>().AsNoTracking()
                join projectTeam in context.Set<ProjectTeam>().AsNoTracking()
                    on allocation.ProjectTeamId equals projectTeam.Id
                join member in context.Set<TeamMember>().AsNoTracking()
                    on new { projectTeam.TeamId, allocation.UserId } equals new { member.TeamId, member.UserId }
                where allocation.ProjectId == folder.ParentResourceId && allocation.UserId == userId &&
                      allocation.IsActive && allocation.DeletedAt == null && allocation.StartsAt <= now &&
                      (!allocation.EndsAt.HasValue || allocation.EndsAt > now) && projectTeam.IsActive &&
                      projectTeam.DeletedAt == null && projectTeam.EndedAt == null && member.IsActive &&
                      member.DeletedAt == null && member.LeftAt == null
                select allocation.Id).AnyAsync(cancellationToken);
    }
}
