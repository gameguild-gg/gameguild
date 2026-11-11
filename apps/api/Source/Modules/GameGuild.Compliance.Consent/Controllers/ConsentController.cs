using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Compliance.Consent;

[Microsoft.AspNetCore.Http.Tags("compliance/consent")]
[ApiController]
[Route("api/compliance/consent")]
[Authorize]
public class ConsentController(ISender sender, IActorContextAccessor actorContextAccessor) : ControllerBase
{
    [HttpGet("policies")]
    public async Task<ActionResult<List<ConsentPolicyDto>>> GetActivePolicies([FromQuery] Guid? tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetActivePoliciesQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPost("policies")]
    public async Task<ActionResult<Guid>> CreatePolicy([FromBody] CreateConsentPolicyCommand command, CancellationToken ct)
        => IsAdministrator()
            ? Ok(await sender.Send(command, ct).ConfigureAwait(false))
            : ForbidPolicyAdministration();

    [HttpPost("policies/{policyId:guid}/versions")]
    public async Task<ActionResult<PolicyVersionDto>> PublishVersion(Guid policyId, [FromBody] PublishVersionRequest request, CancellationToken ct)
        => IsAdministrator()
            ? Ok(await sender.Send(new PublishPolicyVersionCommand(policyId, request.VersionNumber, request.Content, request.ContentType), ct).ConfigureAwait(false))
            : ForbidPolicyAdministration();

    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<List<UserConsentDto>>> GetUserConsents(Guid userId, CancellationToken ct)
        => CanActOnUser(userId)
            ? Ok(await sender.Send(new GetUserConsentsQuery(userId), ct).ConfigureAwait(false))
            : ForbidOwnership(userId);

    [HttpPost("grant")]
    public async Task<ActionResult<UserConsentDto>> GrantConsent([FromBody] GrantConsentCommand command, CancellationToken ct)
        => CanActOnUser(command.UserId)
            ? Ok(await sender.Send(command, ct).ConfigureAwait(false))
            : ForbidOwnership(command.UserId);

    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeConsent([FromBody] RevokeConsentCommand command, CancellationToken ct)
    {
        if (!CanActOnUser(command.UserId))
        {
            return ForbidOwnership(command.UserId);
        }

        await sender.Send(command, ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("data-subject-requests")]
    public async Task<ActionResult<DataSubjectRequestDto>> SubmitRequest([FromBody] SubmitDataSubjectRequestCommand command, CancellationToken ct)
        => CanActOnUser(command.UserId)
            ? Ok(await sender.Send(command, ct).ConfigureAwait(false))
            : ForbidOwnership(command.UserId);

    [HttpPost("data-subject-requests/{requestId:guid}/process")]
    public async Task<ActionResult<DataSubjectRequestDto>> ProcessRequest(Guid requestId, [FromBody] ProcessRequestBody body, CancellationToken ct)
    {
        // Issue #1220 N4: the processor is resolved server-side from the authenticated
        // actor. A ProcessedByUserId supplied by the request body is never trusted —
        // previously it let any client attribute the completion to any user.
        var actor = actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid is not { } actorUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Data subject request processing requires an authenticated actor with a resolvable subject identifier.");
        }

        if (!IsAdministrator())
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Data subject request processing is reserved for administrators.");
        }

        try
        {
            return Ok(await sender.Send(new ProcessDataSubjectRequestCommand(requestId, actorUserId, body.Notes), ct).ConfigureAwait(false));
        }
        catch (KeyNotFoundException exception)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: exception.Message);
        }
    }

    [HttpGet("data-subject-requests/pending")]
    public async Task<ActionResult<List<DataSubjectRequestDto>>> GetPendingRequests(CancellationToken ct)
        => IsAdministrator()
            ? Ok(await sender.Send(new GetPendingDataSubjectRequestsQuery(), ct).ConfigureAwait(false))
            : Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Pending data subject requests are visible to administrators only.");

    private bool CanActOnUser(Guid userId)
    {
        var actor = actorContextAccessor.ActorContext;
        return actor.SubjectIdAsGuid == userId || actor.IsSystemAdmin || actor.IsTenantAdmin;
    }

    private bool IsAdministrator()
    {
        var actor = actorContextAccessor.ActorContext;
        return actor.IsSystemAdmin || actor.IsTenantAdmin;
    }

    private ObjectResult ForbidOwnership(Guid userId)
        => Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: $"Consent operations for user {userId} are reserved for the user or an administrator.");

    private ObjectResult ForbidPolicyAdministration()
        => Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: "Consent policy administration is reserved for administrators.");
}

public record PublishVersionRequest(string VersionNumber, string Content, ContentType ContentType = ContentType.Markdown);
public record ProcessRequestBody(string? Notes = null);
