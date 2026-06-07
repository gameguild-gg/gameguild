using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Compliance.FERPA;

[Microsoft.AspNetCore.Http.Tags("compliance/ferpa")]
[ApiController]
[Route("api/compliance/ferpa")]
[Authorize]
public sealed class FerpaController(ISender sender) : ControllerBase
{
    [HttpGet("students/{studentUserId:guid}/records")]
    public async Task<ActionResult<List<FerpaEducationRecordDto>>> GetStudentRecords(Guid studentUserId, CancellationToken ct)
        => Ok(await sender.Send(new GetStudentEducationRecordsQuery(studentUserId), ct).ConfigureAwait(false));

    [HttpGet("students/{studentUserId:guid}/directory-information")]
    public async Task<ActionResult<List<FerpaEducationRecordDto>>> GetDirectoryInformation(Guid studentUserId, CancellationToken ct)
        => Ok(await sender.Send(new GetStudentDirectoryInformationQuery(studentUserId), ct).ConfigureAwait(false));

    [HttpPost("records")]
    public async Task<ActionResult<FerpaEducationRecordDto>> RegisterRecord([FromBody] RegisterEducationRecordCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct).ConfigureAwait(false));

    [HttpGet("directory-policy")]
    public async Task<ActionResult<FerpaDirectoryInformationPolicyDto?>> GetDirectoryPolicy([FromQuery] Guid? tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetDirectoryInformationPolicyQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPut("directory-policy")]
    public async Task<ActionResult<FerpaDirectoryInformationPolicyDto>> UpsertDirectoryPolicy([FromBody] UpsertDirectoryInformationPolicyCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct).ConfigureAwait(false));

    [HttpGet("students/{studentUserId:guid}/consents")]
    public async Task<ActionResult<List<FerpaDisclosureConsentDto>>> GetConsents(Guid studentUserId, CancellationToken ct)
        => Ok(await sender.Send(new GetStudentFerpaConsentsQuery(studentUserId), ct).ConfigureAwait(false));

    [HttpPost("consents")]
    public async Task<ActionResult<FerpaDisclosureConsentDto>> GrantConsent([FromBody] GrantFerpaDisclosureConsentCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct).ConfigureAwait(false));

    [HttpPost("consents/{consentId:guid}/revoke")]
    public async Task<IActionResult> RevokeConsent(Guid consentId, CancellationToken ct)
        => await sender.Send(new RevokeFerpaDisclosureConsentCommand(consentId), ct).ConfigureAwait(false)
            ? NoContent()
            : NotFound();

    [HttpPost("disclosures")]
    public async Task<ActionResult<FerpaDisclosureLogDto>> RecordDisclosure([FromBody] RecordFerpaDisclosureCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct).ConfigureAwait(false));

    [HttpGet("students/{studentUserId:guid}/disclosures")]
    public async Task<ActionResult<List<FerpaDisclosureLogDto>>> GetDisclosures(Guid studentUserId, CancellationToken ct)
        => Ok(await sender.Send(new GetStudentFerpaDisclosureLogsQuery(studentUserId), ct).ConfigureAwait(false));

    [HttpPost("inspection-requests")]
    public async Task<ActionResult<FerpaInspectionRequestDto>> SubmitInspectionRequest([FromBody] SubmitFerpaInspectionRequestCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct).ConfigureAwait(false));

    [HttpPost("inspection-requests/{requestId:guid}/complete")]
    public async Task<ActionResult<FerpaInspectionRequestDto>> CompleteInspectionRequest(Guid requestId, [FromBody] CompleteFerpaInspectionRequestBody body, CancellationToken ct)
        => Ok(await sender.Send(new CompleteFerpaInspectionRequestCommand(requestId, body.ProcessedByUserId, body.Approved, body.Notes), ct).ConfigureAwait(false));

    [HttpGet("inspection-requests/pending")]
    public async Task<ActionResult<List<FerpaInspectionRequestDto>>> GetPendingInspectionRequests(CancellationToken ct)
        => Ok(await sender.Send(new GetPendingFerpaInspectionRequestsQuery(), ct).ConfigureAwait(false));
}

public sealed record CompleteFerpaInspectionRequestBody(Guid ProcessedByUserId, bool Approved, string? Notes = null);
