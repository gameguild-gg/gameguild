using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.TestingLab;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing/events")]
[Authorize]
public sealed class TestingEventsController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TestingEventProjection>>> GetEvents(
        [FromQuery] TestingEventStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new GetTestingEventsQuery(status, skip, take), cancellationToken).ConfigureAwait(false));

    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<TestingEventProjection>> GetEvent(Guid eventId, CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new GetTestingEventQuery(eventId), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    public async Task<ActionResult<TestingEventProjection>> CreateEvent(
        CreateTestingEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CreateTestingEventCommand(
            request.Name,
            request.Description,
            request.Mode,
            request.ApprovalMode,
            request.ApplicationsOpenAt,
            request.ApplicationsCloseAt,
            request.StartsAt,
            request.EndsAt,
            request.RequiresFeedback), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetEvent), new { eventId = result.Value.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{eventId:guid}")]
    public async Task<ActionResult<TestingEventProjection>> UpdateEvent(
        Guid eventId,
        UpdateTestingEventRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new UpdateTestingEventCommand(
            eventId,
            request.Name,
            request.Description,
            request.Mode,
            request.ApprovalMode,
            request.ApplicationsOpenAt,
            request.ApplicationsCloseAt,
            request.StartsAt,
            request.EndsAt,
            request.RequiresFeedback), cancellationToken).ConfigureAwait(false));

    [HttpDelete("{eventId:guid}")]
    public async Task<ActionResult<bool>> DeleteEvent(Guid eventId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new DeleteTestingEventCommand(eventId), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{eventId:guid}:open-applications")]
    public async Task<ActionResult<TestingEventProjection>> OpenApplications(Guid eventId, CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new OpenTestingEventApplicationsCommand(eventId), cancellationToken).ConfigureAwait(false));

    [HttpPost("{eventId:guid}:close-applications")]
    public async Task<ActionResult<TestingEventProjection>> CloseApplications(Guid eventId, CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new CloseTestingEventApplicationsCommand(eventId), cancellationToken).ConfigureAwait(false));

    [HttpPut("{eventId:guid}/learning")]
    public async Task<ActionResult<TestingEventProjection>> ConfigureLearning(
        Guid eventId,
        ConfigureTestingEventLearningRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new ConfigureTestingEventLearningCommand(
            eventId,
            request.CourseId,
            request.CohortId,
            request.LearningActivityId,
            request.Requirement),
            cancellationToken).ConfigureAwait(false));

    [HttpGet("{eventId:guid}/slots")]
    public async Task<ActionResult<IReadOnlyList<TestingEventSlotProjection>>> GetSlots(
        Guid eventId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new GetTestingEventSlotsQuery(eventId), cancellationToken).ConfigureAwait(false));

    [HttpPost("{eventId:guid}/slots")]
    public async Task<ActionResult<TestingEventSlotProjection>> CreateSlot(
        Guid eventId,
        UpsertTestingEventSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CreateTestingEventSlotCommand(
            eventId,
            request.Mode,
            request.StartsAt,
            request.EndsAt,
            request.MaxTesters,
            request.MaxProjects,
            request.CampusName,
            request.RoomName,
            request.MeetingUrl,
            request.LocationId), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetSlots), new { eventId }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{eventId:guid}/slots/{slotId:guid}")]
    public async Task<ActionResult<TestingEventSlotProjection>> UpdateSlot(
        Guid eventId,
        Guid slotId,
        UpsertTestingEventSlotRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new UpdateTestingEventSlotCommand(
            eventId,
            slotId,
            request.Mode,
            request.StartsAt,
            request.EndsAt,
            request.MaxTesters,
            request.MaxProjects,
            request.CampusName,
            request.RoomName,
            request.MeetingUrl,
            request.LocationId), cancellationToken).ConfigureAwait(false));

    [HttpDelete("{eventId:guid}/slots/{slotId:guid}")]
    public async Task<ActionResult<bool>> DeleteSlot(Guid eventId, Guid slotId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new DeleteTestingEventSlotCommand(eventId, slotId), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    [HttpPost("{eventId:guid}/applications")]
    public async Task<ActionResult<TestingProjectApplicationProjection>> SubmitApplication(
        Guid eventId,
        SubmitTestingProjectApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new SubmitTestingProjectApplicationCommand(
            eventId,
            request.ProjectId,
            request.ProjectVersionId,
            request.PreferredAvailability), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetApplication), new { applicationId = result.Value.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpGet("{eventId:guid}/applications")]
    public async Task<ActionResult<IReadOnlyList<TestingProjectApplicationProjection>>> GetApplications(
        Guid eventId,
        [FromQuery] TestingApplicationStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new GetTestingEventApplicationsQuery(eventId, status, skip, take), cancellationToken).ConfigureAwait(false));

    [HttpGet("applications/{applicationId:guid}")]
    public async Task<ActionResult<TestingProjectApplicationProjection>> GetApplication(
        Guid applicationId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new GetTestingProjectApplicationQuery(applicationId), cancellationToken).ConfigureAwait(false));

    [HttpPost("applications/{applicationId:guid}:withdraw")]
    public async Task<ActionResult<TestingProjectApplicationProjection>> WithdrawApplication(
        Guid applicationId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new WithdrawTestingProjectApplicationCommand(applicationId), cancellationToken).ConfigureAwait(false));

    [HttpPost("applications/{applicationId:guid}:review")]
    public async Task<ActionResult<TestingProjectApplicationProjection>> BeginReview(
        Guid applicationId,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new BeginReviewTestingProjectApplicationCommand(applicationId), cancellationToken).ConfigureAwait(false));

    [HttpPost("applications/{applicationId:guid}/votes")]
    public async Task<ActionResult<TestingApplicationVoteProjection>> CastVote(
        Guid applicationId,
        CastTestingApplicationVoteRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new CastTestingApplicationVoteCommand(
            applicationId,
            request.Decision,
            request.Comments), cancellationToken).ConfigureAwait(false));

    [HttpPost("applications/{applicationId:guid}:approve")]
    public async Task<ActionResult<TestingProjectApplicationProjection>> ApproveApplication(
        Guid applicationId,
        DecideTestingProjectApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.SlotId.HasValue)
            return BadRequest(Error.Validation("TestingLab.SlotRequired", "A slot is required to approve an application."));
        return ToActionResult(await mediator.Send(new ApproveTestingProjectApplicationCommand(
            applicationId,
            request.SlotId.Value,
            request.Rationale), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("applications/{applicationId:guid}:reject")]
    public async Task<ActionResult<TestingProjectApplicationProjection>> RejectApplication(
        Guid applicationId,
        DecideTestingProjectApplicationRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new RejectTestingProjectApplicationCommand(
            applicationId,
            request.Rationale ?? string.Empty), cancellationToken).ConfigureAwait(false));

    [HttpPost("applications/{applicationId:guid}:waitlist")]
    public async Task<ActionResult<TestingProjectApplicationProjection>> WaitlistApplication(
        Guid applicationId,
        DecideTestingProjectApplicationRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new WaitlistTestingProjectApplicationCommand(
            applicationId,
            request.Rationale), cancellationToken).ConfigureAwait(false));

    [HttpPut("applications/{applicationId:guid}/slot")]
    public async Task<ActionResult<TestingProjectApplicationProjection>> AssignSlot(
        Guid applicationId,
        AssignTestingProjectApplicationSlotRequest request,
        CancellationToken cancellationToken = default)
        => ToActionResult(await mediator.Send(new AssignTestingProjectApplicationSlotCommand(
            applicationId,
            request.SlotId), cancellationToken).ConfigureAwait(false));

}
