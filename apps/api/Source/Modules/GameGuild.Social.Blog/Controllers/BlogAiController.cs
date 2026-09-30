using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Finance.Economy.Integrations.AI;
using GameGuild.Identity.Context.Actors;
using GameGuild.Social.Blog.Authoring;
using GameGuild.Social.Blog.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Social.Blog.Controllers;

/// <summary>Blog AI authoring copilot endpoints. All actions require an authenticated author actor.</summary>
[ApiController]
[Authorize]
[Route("api/social/blog/posts/{postId:guid}/ai")]
public sealed class BlogAiController(
    IBlogAuthoringAiService aiAuthoring,
    IActorContextAccessor actorContextAccessor,
    ISender sender) : ControllerBase
{
    /// <summary>Returns the acting author's AI credit wallet snapshot.</summary>
    [HttpGet("entitlement")]
    [ProducesResponseType<BlogAiEntitlementDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BlogAiEntitlementDto>> GetEntitlement(
        Guid postId,
        CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = RequireActor();
        try
        {
            return Ok(await aiAuthoring.GetEntitlement(tenantId, actorId, cancellationToken).ConfigureAwait(false));
        }
        catch (InsufficientAiCreditsException)
        {
            return Ok(new BlogAiEntitlementDto(0, 0, 0));
        }
    }

    /// <summary>Lists the author's copilot conversations for the post.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType<IReadOnlyList<BlogAiConversationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BlogAiConversationDto>>> GetConversations(
        Guid postId,
        CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = RequireActor();
        return Ok(await aiAuthoring.GetConversations(tenantId, actorId, postId, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Starts (or idempotently replays) an AI run against the post's current revision.</summary>
    [HttpPost("runs")]
    [ProducesResponseType<BlogAiRunDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<BlogAiRunDto>> CreateRun(
        Guid postId,
        [FromBody] BlogAiRunRequest request,
        CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = RequireActor();
        try
        {
            var run = await sender.Send(new CreateBlogAiRunCommand(tenantId, actorId, postId, request), cancellationToken).ConfigureAwait(false);
            return AcceptedAtAction(nameof(GetRun), new { postId, runId = run.Id }, run);
        }
        catch (InsufficientAiCreditsException exception)
        {
            return StatusCode(StatusCodes.Status402PaymentRequired, ErrorResponse(
                StatusCodes.Status402PaymentRequired,
                "INSUFFICIENT_AI_CREDITS",
                exception.Message));
        }
        catch (BlogAiExecutionException exception) when (exception.Code.Contains("Quota", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, ErrorResponse(
                StatusCodes.Status429TooManyRequests,
                "AI_QUOTA_EXCEEDED",
                exception.Message));
        }
        catch (BlogRevisionConflictException conflict)
        {
            return ConflictResponse("BLOG_REVISION_CONFLICT", conflict.Message, conflict.CurrentRevision);
        }
        catch (BlogAiIdempotencyConflictException conflict)
        {
            return Conflict(ErrorResponse(
                StatusCodes.Status409Conflict,
                "AI_IDEMPOTENCY_CONFLICT",
                conflict.Message));
        }
        catch (BlogAiProposalKindNotAllowedException exception)
        {
            return BadRequest(ErrorResponse(
                StatusCodes.Status400BadRequest,
                "AI_PROPOSAL_KIND_NOT_ALLOWED",
                exception.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Fetches one run owned by the acting author.</summary>
    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType<BlogAiRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogAiRunDto>> GetRun(
        Guid postId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = RequireActor();
        try
        {
            return Ok(await aiAuthoring.GetRun(tenantId, actorId, postId, runId, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Cancels a queued, reserved, or running run and releases its reservation.</summary>
    [HttpPost("runs/{runId:guid}/cancel")]
    [ProducesResponseType<BlogAiRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogAiRunDto>> CancelRun(
        Guid postId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = RequireActor();
        try
        {
            return Ok(await sender.Send(new CancelBlogAiRunCommand(tenantId, actorId, postId, runId), cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Streams persisted run events (SSE) until the run reaches a terminal state.</summary>
    [HttpGet("runs/{runId:guid}/stream")]
    [Produces("text/event-stream")]
    public async Task StreamRun(
        Guid postId,
        Guid runId,
        [FromHeader(Name = "Last-Event-ID")] long? lastEventId,
        CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = RequireActor();
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Append("X-Accel-Buffering", "no");
        await foreach (var streamEvent in aiAuthoring.StreamRun(
                           tenantId,
                           actorId,
                           postId,
                           runId,
                           lastEventId ?? 0,
                           cancellationToken).ConfigureAwait(false))
        {
            await Response.WriteAsync($"id: {streamEvent.Sequence}\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"event: {streamEvent.Type}\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"data: {SerializeStreamEvent(streamEvent)}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Applies a pending proposal to the post (revision-guarded).</summary>
    [HttpPost("proposals/{proposalId:guid}/apply")]
    [ProducesResponseType<BlogPostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BlogPostDto>> ApplyProposal(
        Guid postId,
        Guid proposalId,
        [FromBody] ApplyBlogAiProposalRequest request,
        CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = RequireActor();
        try
        {
            return Ok(await sender.Send(new ApplyBlogAiProposalCommand(tenantId, actorId, postId, proposalId, request), cancellationToken).ConfigureAwait(false));
        }
        catch (BlogRevisionConflictException conflict)
        {
            return ConflictResponse("BLOG_REVISION_CONFLICT", conflict.Message, conflict.CurrentRevision);
        }
        catch (BlogAiProposalStateConflictException conflict)
        {
            return Conflict(ErrorResponse(
                StatusCodes.Status409Conflict,
                "AI_PROPOSAL_ALREADY_RESOLVED",
                conflict.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Discards a pending proposal without changing the post.</summary>
    [HttpDelete("proposals/{proposalId:guid}")]
    [ProducesResponseType<BlogAiProposalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BlogAiProposalDto>> DiscardProposal(
        Guid postId,
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var (tenantId, actorId) = RequireActor();
        try
        {
            return Ok(await sender.Send(new DiscardBlogAiProposalCommand(tenantId, actorId, postId, proposalId), cancellationToken).ConfigureAwait(false));
        }
        catch (BlogAiProposalStateConflictException conflict)
        {
            return Conflict(ErrorResponse(
                StatusCodes.Status409Conflict,
                "AI_PROPOSAL_ALREADY_RESOLVED",
                conflict.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private (Guid TenantId, Guid ActorId) RequireActor()
    {
        var actor = actorContextAccessor.ActorContext;
        return actor is { IsAuthenticated: true, ActorKind: ActorKind.User, TenantId: { } tenantId, SubjectIdAsGuid: { } actorId }
            ? (tenantId, actorId)
            : throw new UnauthorizedAccessException("An authenticated tenant user actor is required.");
    }

    private static ProblemDetails ErrorResponse(int status, string code, string detail) => new()
    {
        Status = status,
        Title = "AI authoring request rejected",
        Detail = detail,
        Extensions = { ["code"] = code },
    };

    private static string SerializeStreamEvent(BlogAiStreamEventDto streamEvent) =>
        JsonSerializer.Serialize(streamEvent, JsonSerializerOptions.Web);

    private ConflictObjectResult ConflictResponse(string code, string detail, int currentRevision) => Conflict(new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Blog conflict",
        Detail = detail,
        Extensions =
        {
            ["code"] = code,
            ["currentRevision"] = currentRevision,
        },
    });
}
