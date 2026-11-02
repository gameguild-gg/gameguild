using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Social.Mentorship;

/// <summary>
///     Public mentor directory (issue #102): advisees browse published mentors and
///     advisors, optionally filtered by skill, and see their open availability.
/// </summary>
[Microsoft.AspNetCore.Http.Tags("social/mentorship")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/mentorship/mentors")]
public sealed class MentorsController(ISender sender) : BaseApiController
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<MentorProfileDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MentorProfileDto>> List(
        [FromQuery] string? skill,
        [FromQuery] MentorshipProgramRole? role,
        [FromQuery] string? search,
        [FromQuery] bool? offersIntroductoryCall,
        [FromQuery] int skip,
        [FromQuery] int take,
        CancellationToken cancellationToken)
        => sender.Send<IReadOnlyList<MentorProfileDto>>(
            new ListMentorsQuery(skill, role, search, offersIntroductoryCall, skip, take),
            cancellationToken);

    [HttpGet("{profileId:guid}", Name = "GetMentorProfileById")]
    [AllowAnonymous]
    [ProducesResponseType<MentorProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MentorProfileDto>> Get(Guid profileId, CancellationToken cancellationToken)
    {
        var mentor = await sender.Send(new GetMentorQuery(profileId), cancellationToken).ConfigureAwait(false);
        return mentor is null ? NotFound() : Ok(mentor);
    }

    [HttpGet("{profileId:guid}/availability")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<MentorAvailabilitySlotDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MentorAvailabilitySlotDto>> Availability(
        Guid profileId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
        => sender.Send<IReadOnlyList<MentorAvailabilitySlotDto>>(
            new ListMentorAvailabilityQuery(profileId, fromUtc, toUtc),
            cancellationToken);
}
