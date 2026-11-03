using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Compliance.FERPA;

[Microsoft.AspNetCore.Http.Tags("compliance/ferpa")]
[ApiController]
[Route("api/compliance/ferpa")]
[Authorize]
public sealed class FerpaController(ISender sender, IActorContextAccessor actorContextAccessor) : ControllerBase
{
    [HttpGet("students/{studentUserId:guid}/records")]
    public async Task<ActionResult<List<FerpaEducationRecordDto>>> GetStudentRecords(Guid studentUserId, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(new GetStudentEducationRecordsQuery(studentUserId), ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpGet("students/{studentUserId:guid}/directory-information")]
    public async Task<ActionResult<List<FerpaEducationRecordDto>>> GetDirectoryInformation(Guid studentUserId, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(new GetStudentDirectoryInformationQuery(studentUserId), ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpPost("records")]
    public async Task<ActionResult<FerpaEducationRecordDto>> RegisterRecord([FromBody] RegisterEducationRecordCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(command, ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpGet("directory-policy")]
    public async Task<ActionResult<FerpaDirectoryInformationPolicyDto?>> GetDirectoryPolicy([FromQuery] Guid? tenantId, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(new GetDirectoryInformationPolicyQuery(tenantId), ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpPut("directory-policy")]
    public async Task<ActionResult<FerpaDirectoryInformationPolicyDto>> UpsertDirectoryPolicy([FromBody] UpsertDirectoryInformationPolicyCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(command, ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpGet("students/{studentUserId:guid}/consents")]
    public async Task<ActionResult<List<FerpaDisclosureConsentDto>>> GetConsents(Guid studentUserId, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(new GetStudentFerpaConsentsQuery(studentUserId), ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpPost("consents")]
    public async Task<ActionResult<FerpaDisclosureConsentDto>> GrantConsent([FromBody] GrantFerpaDisclosureConsentCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(command, ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpPost("consents/{consentId:guid}/revoke")]
    public async Task<IActionResult> RevokeConsent(Guid consentId, CancellationToken ct)
    {
        try
        {
            return await sender.Send(new RevokeFerpaDisclosureConsentCommand(consentId), ct).ConfigureAwait(false)
                ? NoContent()
                : NotFound();
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpPost("disclosures")]
    public async Task<ActionResult<FerpaDisclosureLogDto>> RecordDisclosure([FromBody] RecordFerpaDisclosureCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(command, ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpGet("students/{studentUserId:guid}/disclosures")]
    public async Task<ActionResult<List<FerpaDisclosureLogDto>>> GetDisclosures(Guid studentUserId, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(new GetStudentFerpaDisclosureLogsQuery(studentUserId), ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpPost("inspection-requests")]
    public async Task<ActionResult<FerpaInspectionRequestDto>> SubmitInspectionRequest([FromBody] SubmitFerpaInspectionRequestCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(command, ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpPost("inspection-requests/{requestId:guid}/complete")]
    public async Task<ActionResult<FerpaInspectionRequestDto>> CompleteInspectionRequest(Guid requestId, [FromBody] CompleteFerpaInspectionRequestBody body, CancellationToken ct)
    {
        // Issue #1220 N4: the processor is resolved server-side from the authenticated
        // actor. A ProcessedByUserId supplied by the request body is never trusted —
        // previously it allowed any client to attribute the decision to any user.
        if (actorContextAccessor.ActorContext.SubjectIdAsGuid is not { } actorUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "FERPA mutations require an authenticated actor with a resolvable subject identifier.");
        }

        try
        {
            return Ok(await sender.Send(
                new CompleteFerpaInspectionRequestCommand(requestId, actorUserId, body.Approved, body.Notes),
                ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    [HttpGet("inspection-requests/pending")]
    public async Task<ActionResult<List<FerpaInspectionRequestDto>>> GetPendingInspectionRequests(CancellationToken ct)
    {
        try
        {
            return Ok(await sender.Send(new GetPendingFerpaInspectionRequestsQuery(), ct).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return MapFailure(exception);
        }
    }

    /// <summary>
    ///     The FERPA guard-failure mapping the controller previously lacked
    ///     (issue #1220 N5): ownership/tenant-scope failures surface as 403 instead of
    ///     unhandled 500s, missing entities as 404 and state-machine conflicts (e.g. a
    ///     disclosure without an active consent) as 409. Anything else rethrows so the
    ///     platform's general exception handling stays in charge.
    /// </summary>
    private ObjectResult MapFailure(Exception exception) => exception switch
    {
        FerpaAuthorizationException failure => Problem(
            statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: failure.Message),
        KeyNotFoundException missing => Problem(
            statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: missing.Message),
        InvalidOperationException conflict => Problem(
            statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: conflict.Message),
        _ => throw exception
    };
}

public sealed record CompleteFerpaInspectionRequestBody(bool Approved, string? Notes = null);
