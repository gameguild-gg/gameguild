using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Compliance.Consent;

[ApiController]
[Route("api/compliance/consent")]
[Authorize]
public class ConsentController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("policies")]
    public async Task<ActionResult<List<ConsentPolicyDto>>> GetActivePolicies([FromQuery] Guid? tenantId, CancellationToken ct)
        => Ok(await dispatcher.SendAsync(new GetActivePoliciesQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPost("policies")]
    public async Task<ActionResult<Guid>> CreatePolicy([FromBody] CreateConsentPolicyCommand command, CancellationToken ct)
        => Ok(await dispatcher.SendAsync(command, ct).ConfigureAwait(false));

    [HttpPost("policies/{policyId:guid}/versions")]
    public async Task<ActionResult<PolicyVersionDto>> PublishVersion(Guid policyId, [FromBody] PublishVersionRequest request, CancellationToken ct)
        => Ok(await dispatcher.SendAsync(new PublishPolicyVersionCommand(policyId, request.VersionNumber, request.Content, request.ContentType), ct).ConfigureAwait(false));

    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<List<UserConsentDto>>> GetUserConsents(Guid userId, CancellationToken ct)
        => Ok(await dispatcher.SendAsync(new GetUserConsentsQuery(userId), ct).ConfigureAwait(false));

    [HttpPost("grant")]
    public async Task<ActionResult<UserConsentDto>> GrantConsent([FromBody] GrantConsentCommand command, CancellationToken ct)
        => Ok(await dispatcher.SendAsync(command, ct).ConfigureAwait(false));

    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeConsent([FromBody] RevokeConsentCommand command, CancellationToken ct)
    {
        await dispatcher.SendAsync(command, ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("data-subject-requests")]
    public async Task<ActionResult<DataSubjectRequestDto>> SubmitRequest([FromBody] SubmitDataSubjectRequestCommand command, CancellationToken ct)
        => Ok(await dispatcher.SendAsync(command, ct).ConfigureAwait(false));

    [HttpPost("data-subject-requests/{requestId:guid}/process")]
    public async Task<ActionResult<DataSubjectRequestDto>> ProcessRequest(Guid requestId, [FromBody] ProcessRequestBody body, CancellationToken ct)
        => Ok(await dispatcher.SendAsync(new ProcessDataSubjectRequestCommand(requestId, body.ProcessedByUserId, body.Notes), ct).ConfigureAwait(false));

    [HttpGet("data-subject-requests/pending")]
    public async Task<ActionResult<List<DataSubjectRequestDto>>> GetPendingRequests(CancellationToken ct)
        => Ok(await dispatcher.SendAsync(new GetPendingDataSubjectRequestsQuery(), ct).ConfigureAwait(false));
}

public record PublishVersionRequest(string VersionNumber, string Content, ContentType ContentType = ContentType.Markdown);
public record ProcessRequestBody(Guid ProcessedByUserId, string? Notes = null);
