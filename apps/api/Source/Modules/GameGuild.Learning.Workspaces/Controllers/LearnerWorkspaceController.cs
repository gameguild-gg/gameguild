using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Learning.Workspaces;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/learning")]
[Authorize]
public sealed class LearnerWorkspaceController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpGet("me/dashboard")]
    [ProducesResponseType<LearnerDashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LearnerDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        if (actorContextAccessor.ActorContext.SubjectIdAsGuid is not Guid userId)
        {
            return Unauthorized();
        }

        var dashboard = await sender
            .Send<LearnerDashboardDto>(new GetLearnerDashboardQuery(userId), cancellationToken)
            .ConfigureAwait(false);
        return Ok(dashboard);
    }

    [HttpGet("courses/{courseId:guid}/workspace")]
    [ProducesResponseType<LearnerCourseWorkspaceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnerCourseWorkspaceDto>> GetCourseWorkspace(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (actorContextAccessor.ActorContext.SubjectIdAsGuid is not Guid userId)
        {
            return Unauthorized();
        }

        var workspace = await sender
            .Send<LearnerCourseWorkspaceDto?>(
                new GetLearnerCourseWorkspaceQuery(userId, courseId),
                cancellationToken)
            .ConfigureAwait(false);
        return workspace is null ? NotFound() : Ok(workspace);
    }

    [HttpGet("me/search")]
    [ProducesResponseType<IReadOnlyList<LearnerSearchResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<LearnerSearchResultDto>>> Search(
        [FromQuery] string q,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (actorContextAccessor.ActorContext.SubjectIdAsGuid is not Guid userId)
        {
            return Unauthorized();
        }

        var results = await sender
            .Send<IReadOnlyList<LearnerSearchResultDto>>(
                new SearchLearnerWorkspaceQuery(userId, q ?? string.Empty, take),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(results);
    }
}
