using Asp.Versioning;
using GameGuild.Projects;
using GameGuild.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.Teams;

public sealed record TeamProjectSummary(
    Guid Id,
    string Title,
    string Slug,
    ContentStatus Status,
    ContentVisibility Visibility,
    ProjectTeamRole TeamRole,
    ProjectTeamParticipationMode ParticipationMode,
    DateTime UpdatedAt);

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("v{version:apiVersion}/teams/{teamId:guid}/projects")]
public sealed class TeamProjectsController(
    IApplicationDbContext context,
    ITeamAuthorizationService teamAuthorization,
    IProjectAuthorizationService projectAuthorization) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamProjectSummary>>> List(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        if (!await teamAuthorization.HasAuthorityAsync(teamId, TeamMemberAuthority.Viewer, cancellationToken).ConfigureAwait(false))
            return NotFound();

        var rows = await projectAuthorization.ApplyWorkspaceAccess(
                context.Set<Project>().AsNoTracking().Where(project => project.DeletedAt == null))
            .SelectMany(project => project.Teams
                .Where(team => team.TeamId == teamId && team.IsActive && team.EndedAt == null && team.DeletedAt == null)
                .Select(team => new TeamProjectSummary(
                    project.Id,
                    project.Title,
                    project.Slug,
                    project.Status,
                    project.Visibility,
                    team.Role,
                    team.ParticipationMode,
                    project.UpdatedAt)))
            .OrderByDescending(project => project.UpdatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }
}
