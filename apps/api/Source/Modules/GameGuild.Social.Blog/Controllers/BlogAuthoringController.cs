using GameGuild.CQRS;
using GameGuild.Social.Blog.Commands;
using GameGuild.Social.Blog.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Social.Blog.Controllers;

/// <summary>
/// Authenticated blog authoring surface: draft CRUD, revision-guarded updates, slug changes,
/// co-author management, primary transfer, publish/unpublish, delete. Actor identity always
/// comes from the actor context inside the command handlers (invariant #8), never from the
/// request body.
/// </summary>
[ApiController]
[Authorize]
[Route("api/social/blog/posts")]
public sealed class BlogAuthoringController(ISender sender) : ControllerBase
{
    /// <summary>Creates a draft post; the actor becomes the primary author.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BlogPost), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBlogPostRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await sender.Send(
                new CreateBlogPostCommand(request.Title, request.Format),
                cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>Lists the acting user's posts (authored + co-authored), newest edit first.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<BlogPost>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(
        [FromQuery] int page = 0,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new ListMyBlogPostsQuery(page), cancellationToken).ConfigureAwait(false));

    /// <summary>Fetches one post for an author (primary or co-author) with full draft access.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(new GetBlogPostForAuthorQuery(id), cancellationToken).ConfigureAwait(false)
                is { } post
                ? Ok(post)
                : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>Revision-guarded draft update; any author (primary or co-author). Stale revision → 409.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBlogPostDraftRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await sender.Send(
                new UpdateBlogPostDraftCommand(
                    id,
                    request.Revision,
                    request.Title,
                    request.Content,
                    request.JsonBody,
                    request.Excerpt,
                    request.Tags,
                    request.MetaTitle,
                    request.MetaDescription,
                    request.OgImageUrl,
                    request.CanonicalUrlOverride,
                    request.TwitterCard,
                    request.StructuredDataOverride,
                    request.AllowComments),
                cancellationToken).ConfigureAwait(false);
            return Ok(post);
        }
        catch (BlogRevisionConflictException conflict)
        {
            return ConflictRevision(conflict);
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

    /// <summary>Changes the post slug (primary only); the old route 301-redirects forever.</summary>
    [HttpPost("{id:guid}/slug")]
    [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeSlug(
        Guid id,
        [FromBody] ChangeBlogPostSlugRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await sender.Send(
                new ChangeBlogPostSlugCommand(id, request.NewSlug),
                cancellationToken).ConfigureAwait(false));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(ErrorResponse(StatusCodes.Status400BadRequest, "BLOG_SLUG_INVALID", exception.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Adds a co-author (primary only).</summary>
    [HttpPost("{id:guid}/coauthors")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCoauthor(
        Guid id,
        [FromBody] BlogCoauthorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new AddBlogPostCoauthorCommand(id, request.UserId), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(ErrorResponse(StatusCodes.Status400BadRequest, "BLOG_COAUTHOR_INVALID", exception.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Removes a co-author (primary only).</summary>
    [HttpDelete("{id:guid}/coauthors/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCoauthor(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new RemoveBlogPostCoauthorCommand(id, userId), cancellationToken).ConfigureAwait(false);
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

    /// <summary>Transfers primary authorship to a current co-author (primary only).</summary>
    [HttpPost("{id:guid}/transfer-primary")]
    [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferPrimary(
        Guid id,
        [FromBody] TransferBlogPrimaryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await sender.Send(
                new TransferBlogPrimaryCommand(id, request.NewPrimaryUserId),
                cancellationToken).ConfigureAwait(false));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(ErrorResponse(StatusCodes.Status400BadRequest, "BLOG_TRANSFER_INVALID", exception.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Publishes the post (primary only); fans out the publication announcement.</summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await sender.Send(new PublishBlogPostCommand(id), cancellationToken).ConfigureAwait(false));
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

    /// <summary>Unpublishes the post back to draft (primary only).</summary>
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await sender.Send(new UnpublishBlogPostCommand(id), cancellationToken).ConfigureAwait(false));
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

    /// <summary>Soft-deletes the post (primary only).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteBlogPostCommand(id), cancellationToken).ConfigureAwait(false);
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

    private static ProblemDetails ErrorResponse(int status, string code, string detail) => new()
    {
        Status = status,
        Title = "Blog request rejected",
        Detail = detail,
        Extensions = { ["code"] = code },
    };

    private ConflictObjectResult ConflictRevision(BlogRevisionConflictException conflict) => Conflict(new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Blog revision conflict",
        Detail = conflict.Message,
        Extensions =
        {
            ["code"] = "BLOG_REVISION_CONFLICT",
            ["expectedRevision"] = conflict.ExpectedRevision,
            ["currentRevision"] = conflict.CurrentRevision,
        },
    });
}

/// <summary>Wire shape for <see cref="BlogAuthoringController.Create"/>; actor identity and tenant are never accepted from the body.</summary>
public sealed record CreateBlogPostRequest(string Title, BlogContentFormat Format);

/// <summary>Wire shape for <see cref="BlogAuthoringController.Update"/>; only patch fields and the expected revision.</summary>
public sealed record UpdateBlogPostDraftRequest(
    int Revision,
    string? Title = null,
    string? Content = null,
    string? JsonBody = null,
    string? Excerpt = null,
    IReadOnlyList<string>? Tags = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? OgImageUrl = null,
    string? CanonicalUrlOverride = null,
    string? TwitterCard = null,
    string? StructuredDataOverride = null,
    bool? AllowComments = null);

/// <summary>Wire shape for <see cref="BlogAuthoringController.ChangeSlug"/>.</summary>
public sealed record ChangeBlogPostSlugRequest(string NewSlug);

/// <summary>Wire shape for <see cref="BlogAuthoringController.AddCoauthor"/>.</summary>
public sealed record BlogCoauthorRequest(Guid UserId);

/// <summary>Wire shape for <see cref="BlogAuthoringController.TransferPrimary"/>.</summary>
public sealed record TransferBlogPrimaryRequest(Guid NewPrimaryUserId);
