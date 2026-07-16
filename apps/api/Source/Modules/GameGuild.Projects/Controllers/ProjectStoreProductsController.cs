using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Projects;

[ApiController]
[Authorize]
[Route("v1/projects/{projectId:guid}/store-products")]
public sealed class ProjectStoreProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectStoreProductProjection>>> List(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetProjectStoreProductsQuery(projectId), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectStoreProductProjection>> Link(
        Guid projectId,
        [FromBody] LinkProjectStoreProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new LinkProjectStoreProductCommand(projectId, request.ProductId), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) return ToActionResult(result);
        return CreatedAtAction(nameof(List), new { projectId }, result.Value);
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Unlink(Guid projectId, Guid productId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new UnlinkProjectStoreProductCommand(projectId, productId), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("~/v1/store/products/{productId:guid}/projects")]
    public async Task<ActionResult<IReadOnlyList<ProjectStoreProductProjection>>> ListPublicProductProjects(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPublicStoreProductProjectsQuery(productId), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result);
    }

    private ObjectResult ToActionResult(Result result)
        => result.Error.Type switch
        {
            ErrorType.Unauthorized => StatusCode(StatusCodes.Status401Unauthorized, result.Error),
            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result.Error),
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Conflict => Conflict(result.Error),
            ErrorType.Validation => BadRequest(result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result.Error)
        };
}
