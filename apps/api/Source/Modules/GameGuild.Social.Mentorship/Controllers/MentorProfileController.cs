using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Social.Mentorship;

/// <summary>
///     Self-service mentor profile management (issue #102): mentors and advisors
///     publish their profile and availability; the dashboard summarizes bookings.
/// </summary>
[Microsoft.AspNetCore.Http.Tags("social/mentorship")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/mentorship/profile")]
[Authorize]
public sealed class MentorProfileController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<MentorProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MentorProfileDto>> GetMine(CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var profile = await sender.Send(new GetMyMentorProfileQuery(userId), cancellationToken).ConfigureAwait(false);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType<MentorProfileDto>(StatusCodes.Status200OK)]
    public Task<MentorProfileDto> Upsert(
        [FromBody] UpsertMentorProfileRequest request,
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        return sender.Send<MentorProfileDto>(
            new UpsertMentorProfileCommand(
                GetRequiredUserId(),
                request.DisplayName,
                request.Headline,
                request.Bio,
                request.Role,
                request.PricingModel,
                request.FreeContributionReason,
                request.BookingMode,
                request.OffersIntroductoryCall,
                request.IntroductoryCallMinutes,
                request.TimezoneId,
                request.Skills ?? [],
                actor.TenantId),
            cancellationToken);
    }

    [HttpPost("status")]
    [ProducesResponseType<MentorProfileDto>(StatusCodes.Status200OK)]
    public Task<MentorProfileDto> SetStatus(
        [FromBody] SetMentorProfileStatusRequest request,
        CancellationToken cancellationToken)
        => sender.Send<MentorProfileDto>(
            new SetMentorProfileStatusCommand(GetRequiredUserId(), request.Status),
            cancellationToken);

    [HttpPost("availability")]
    [ProducesResponseType<IReadOnlyList<MentorAvailabilitySlotDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MentorAvailabilitySlotDto>> PublishAvailability(
        [FromBody] PublishMentorAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        return sender.Send<IReadOnlyList<MentorAvailabilitySlotDto>>(
            new PublishMentorAvailabilityCommand(userId, request.Slots ?? []),
            cancellationToken);
    }

    [HttpDelete("availability/{slotId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAvailability(Guid slotId, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var removed = await sender.Send(
                new RemoveMentorAvailabilitySlotCommand(userId, slotId),
                cancellationToken)
            .ConfigureAwait(false);
        return removed ? NoContent() : NotFound();
    }

    [HttpGet("dashboard")]
    [ProducesResponseType<MentorDashboardDto>(StatusCodes.Status200OK)]
    public Task<MentorDashboardDto> Dashboard(CancellationToken cancellationToken)
        => sender.Send<MentorDashboardDto>(
            new GetMentorDashboardQuery(GetRequiredUserId()),
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
