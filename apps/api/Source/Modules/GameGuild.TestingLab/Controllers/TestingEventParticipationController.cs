using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.TestingLab;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing/events")]
[Authorize]
public sealed class TestingEventParticipationController(IMediator mediator) : BaseApiController
{
    [HttpPost("slots/{slotId:guid}/registrations")]
    public async Task<ActionResult<TestingSlotRegistrationProjection>> Register(
        Guid slotId,
        RegisterTestingEventSlotRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new RegisterTestingEventSlotCommand(slotId, request.Notes),
            cancellationToken).ConfigureAwait(false));

    [HttpDelete("registrations/{registrationId:guid}")]
    public async Task<ActionResult<TestingSlotRegistrationProjection>> Cancel(
        Guid registrationId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new CancelTestingEventSlotRegistrationCommand(registrationId),
            cancellationToken).ConfigureAwait(false));

    [HttpGet("registrations/me")]
    public async Task<ActionResult<IReadOnlyList<TestingSlotRegistrationProjection>>> GetMyRegistrations(
        [FromQuery] Guid? eventId = null,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new GetMyTestingSlotRegistrationsQuery(eventId),
            cancellationToken).ConfigureAwait(false));
    [HttpGet("slots/{slotId:guid}/registrations")]
    public async Task<ActionResult<IReadOnlyList<TestingSlotRegistrationProjection>>> GetRegistrations(
        Guid slotId,
        [FromQuery] TestingSlotRegistrationStatus? status = null,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new GetTestingEventSlotRegistrationsQuery(slotId, status),
            cancellationToken).ConfigureAwait(false));

    [HttpPost("registrations/{registrationId:guid}:check-in")]
    public async Task<ActionResult<TestingSlotRegistrationProjection>> CheckIn(
        Guid registrationId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new CheckInTestingEventRegistrationCommand(registrationId),
            cancellationToken).ConfigureAwait(false));

    [HttpPost("registrations/{registrationId:guid}:check-out")]
    public async Task<ActionResult<TestingSlotRegistrationProjection>> CheckOut(
        Guid registrationId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new CheckOutTestingEventRegistrationCommand(registrationId),
            cancellationToken).ConfigureAwait(false));

    [HttpPost("registrations/{registrationId:guid}:no-show")]
    public async Task<ActionResult<TestingSlotRegistrationProjection>> MarkNoShow(
        Guid registrationId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new MarkTestingEventNoShowCommand(registrationId),
            cancellationToken).ConfigureAwait(false));

    [HttpPost("registrations/{registrationId:guid}/tested-projects")]
    public async Task<ActionResult<TestingFeedbackObligationProjection>> AssignTestedProject(
        Guid registrationId,
        AssignTestingProjectToTesterRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new AssignTestingProjectToTesterCommand(registrationId, request.ApplicationId),
            cancellationToken).ConfigureAwait(false));

    [HttpPost("registrations/{registrationId:guid}:complete")]
    public async Task<ActionResult<TestingSlotRegistrationProjection>> Complete(
        Guid registrationId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new CompleteTestingEventParticipationCommand(registrationId),
            cancellationToken).ConfigureAwait(false));

    [HttpGet("feedback-obligations/me")]
    public async Task<ActionResult<IReadOnlyList<TestingFeedbackObligationProjection>>> GetMyFeedbackObligations(
        [FromQuery] Guid? eventId = null,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new GetMyTestingFeedbackObligationsQuery(eventId),
            cancellationToken).ConfigureAwait(false));

    [HttpPost("feedback-obligations/{obligationId:guid}/feedback")]
    public async Task<ActionResult<TestingEventFeedbackProjection>> SubmitFeedback(
        Guid obligationId,
        SubmitTestingEventFeedbackRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(
            new SubmitTestingEventFeedbackCommand(
                obligationId,
                request.FeedbackData,
                request.OverallRating,
                request.WouldRecommend,
                request.AdditionalNotes),
            cancellationToken).ConfigureAwait(false));
}
