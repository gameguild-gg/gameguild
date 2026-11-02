using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Social.Mentorship;

/// <summary>
///     Mentorship session lifecycle (issue #102): advisees book introductory
///     meetings against published availability, and either side can cancel.
/// </summary>
[Microsoft.AspNetCore.Http.Tags("social/mentorship")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/mentorship/sessions")]
[Authorize]
public sealed class MentorshipSessionsController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MentorshipSessionDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MentorshipSessionDto>> List(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] MentorshipSessionStatus? status,
        [FromQuery] bool asMentorOnly,
        CancellationToken cancellationToken)
        => sender.Send<IReadOnlyList<MentorshipSessionDto>>(
            new ListMyMentorshipSessionsQuery(GetRequiredUserId(), fromUtc, toUtc, status, asMentorOnly),
            cancellationToken);

    [HttpPost]
    [ProducesResponseType<MentorshipSessionDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MentorshipSessionDto>> Book(
        [FromBody] BookMentorshipSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await sender.Send<MentorshipSessionDto>(
                new BookMentorshipSessionCommand(
                    GetRequiredUserId(),
                    request.MenteeUserId,
                    request.SlotId,
                    request.Topic,
                    request.Notes,
                    request.MeetingChannel),
                cancellationToken)
            .ConfigureAwait(false);

        return CreatedAtAction(nameof(List), new { id = session.Id }, session);
    }

    [HttpPost("{sessionId:guid}/cancel")]
    [ProducesResponseType<MentorshipSessionDto>(StatusCodes.Status200OK)]
    public Task<MentorshipSessionDto> Cancel(
        Guid sessionId,
        [FromBody] CancelMentorshipSessionRequest request,
        CancellationToken cancellationToken)
        => sender.Send<MentorshipSessionDto>(
            new CancelMentorshipSessionCommand(GetRequiredUserId(), sessionId, request.Reason),
            cancellationToken);

    [HttpPost("{sessionId:guid}/complete")]
    [ProducesResponseType<MentorshipSessionDto>(StatusCodes.Status200OK)]
    public Task<MentorshipSessionDto> Complete(Guid sessionId, CancellationToken cancellationToken)
        => sender.Send<MentorshipSessionDto>(
            new CompleteMentorshipSessionCommand(GetRequiredUserId(), sessionId),
            cancellationToken);

    private Guid GetRequiredUserId()
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A signed-in user is required.");
        }

        return userId.Value;
    }
}
