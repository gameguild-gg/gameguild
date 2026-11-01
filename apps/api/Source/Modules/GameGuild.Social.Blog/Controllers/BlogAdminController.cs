using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Social.Blog.Commands;
using GameGuild.Social.Blog.Queries;
using GameGuild.Social.Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Social.Blog.Controllers;

/// <summary>
/// SystemAdmin blog administration surface (issue #96): platform-wide post/comment moderation
/// (list-all, takedown, deletion, comment removal) and the Ghost content migration importer.
/// Every action is guarded by the SystemAdmin policy AND a fail-closed actor re-check inside
/// the command handlers. The acting moderator identity comes from the actor context only.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.SystemAdmin)]
[Route("api/social/blog/admin")]
public sealed class BlogAdminController(ISender sender) : ControllerBase
{
    /// <summary>Lists every live post (any status, any author), newest edit first.</summary>
    [HttpGet("posts")]
    [ProducesResponseType(typeof(BlogAdminPostPage), StatusCodes.Status200OK)]
    public async Task<IActionResult> Posts(
        [FromQuery] int page = 0,
        [FromQuery] BlogPostStatus? status = null,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new ListBlogAdminPostsQuery(page, status), cancellationToken).ConfigureAwait(false));

    /// <summary>Lists the newest comments platform-wide for moderation.</summary>
    [HttpGet("comments")]
    [ProducesResponseType(typeof(BlogAdminCommentPage), StatusCodes.Status200OK)]
    public async Task<IActionResult> Comments(
        [FromQuery] int page = 0,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new ListBlogAdminCommentsQuery(page), cancellationToken).ConfigureAwait(false));

    /// <summary>Platform-wide blog counters for the admin console header.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(BlogAdminStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetBlogAdminStatsQuery(), cancellationToken).ConfigureAwait(false));

    /// <summary>Force-unpublishes a post regardless of authorship (takedown).</summary>
    [HttpPost("posts/{id:guid}/unpublish")]
    [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await sender.Send(new AdminUnpublishBlogPostCommand(id), cancellationToken).ConfigureAwait(false));
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

    /// <summary>Soft-deletes a post regardless of authorship.</summary>
    [HttpDelete("posts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new AdminDeleteBlogPostCommand(id), cancellationToken).ConfigureAwait(false);
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

    /// <summary>Soft-deletes any comment; the moderator id is recorded on the row.</summary>
    [HttpDelete("comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new AdminDeleteBlogCommentCommand(commentId), cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Imports a Ghost export JSON into the internal blogging system (issue #96 "port all old
    /// content"). Idempotent: re-running skips posts whose (author, slug) already exist. Pass
    /// <c>dryRun: true</c> to plan without persisting.
    /// </summary>
    [HttpPost("import/ghost")]
    [ProducesResponseType(typeof(GhostImportReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportGhost(
        [FromBody] GhostImportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await sender.Send(
                new ImportGhostContentCommand(request.ExportJson, request.DryRun, request.PublishImported),
                cancellationToken).ConfigureAwait(false);
            return Ok(report);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (System.Text.Json.JsonException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Ghost export",
                Detail = exception.Message,
                Extensions = { ["code"] = "GHOST_IMPORT_INVALID" },
            });
        }
    }
}

/// <summary>Wire shape for <see cref="BlogAdminController.ImportGhost"/>; the moderator comes from the actor context.</summary>
public sealed record GhostImportRequest(
    string ExportJson,
    bool DryRun = true,
    bool PublishImported = true);
