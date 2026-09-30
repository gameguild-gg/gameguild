using GameGuild.CQRS;
using GameGuild.Social.Blog.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Social.Blog.Controllers;

/// <summary>
/// Authenticated blog comment writes. Actor identity always comes from
/// <see cref="Identity.Context.Actors.IActorContextAccessor"/> inside the command handlers
/// (invariant #8), never from the request body.
/// </summary>
[ApiController]
[Authorize]
[Route("api/social/blog")]
public sealed class BlogCommentsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Adds a comment on a published post (any authenticated user; depth ≤ 1; block-enforced;
    /// disabled-comment and depth-2 violations map to 400, blocks to 403).
    /// </summary>
    [HttpPost("posts/{id:guid}/comments")]
    [ProducesResponseType(typeof(BlogComment), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(
        Guid id,
        [FromBody] AddBlogCommentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var comment = await sender.Send(
                new AddBlogCommentCommand(id, request.Content, request.ParentCommentId),
                cancellationToken).ConfigureAwait(false);
            return StatusCode(StatusCodes.Status201Created, comment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Comment rejected",
                Detail = exception.Message,
                Extensions = { ["code"] = "BLOG_COMMENT_INVALID" },
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Soft-deletes a comment (comment author, post primary, or any co-author; others → 403).
    /// </summary>
    [HttpDelete("comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid commentId, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteBlogCommentCommand(commentId), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

/// <summary>Wire shape for <see cref="BlogCommentsController.Add"/>; the author comes from the actor context.</summary>
public sealed record AddBlogCommentRequest(string Content, Guid? ParentCommentId = null);
