using Asp.Versioning;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Actor-scoped to-do aggregation. Everything is derived from the authenticated actor —
/// no course ids are accepted from client input.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/me/tasks")]
[Authorize]
public class TasksController(
    ITasksService tasksService,
    IActorContextAccessor actorContextAccessor,
    ILogger<TasksController> logger) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<TasksDto>> GetTasks()
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.SubjectIdAsGuid.HasValue)
        {
            logger.LogWarning("Actor without a guid subject requested tasks");
            return Ok(new TasksDto([]));
        }

        var tasks = await tasksService
            .GetTasksAsync(actor.SubjectIdAsGuid.Value, actor.TenantId, actor.IsSystemAdmin)
            .ConfigureAwait(false);
        return Ok(tasks);
    }
}
