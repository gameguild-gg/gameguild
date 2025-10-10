using Microsoft.AspNetCore.Mvc;
using GameGuild.Modules.Compliance.Services;

namespace GameGuild.Modules.Compliance.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceService _complianceService;
    private readonly IConsentService _consentService;

    public ComplianceController(
        IComplianceService complianceService,
        IConsentService consentService)
    {
        _complianceService = complianceService;
        _consentService = consentService;
    }

    [HttpPost("policies")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _complianceService.CreatePolicyAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("policies/{policyId}")]
    public async Task<IActionResult> UpdatePolicy(Guid policyId, [FromBody] UpdatePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _complianceService.UpdatePolicyAsync(policyId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("policies/{policyId}/publish")]
    public async Task<IActionResult> PublishPolicy(Guid policyId, [FromQuery] Guid versionId, CancellationToken cancellationToken)
    {
        var result = await _complianceService.PublishPolicyAsync(policyId, versionId, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("policies/{policyId}/deactivate")]
    public async Task<IActionResult> DeactivatePolicy(Guid policyId, CancellationToken cancellationToken)
    {
        var result = await _complianceService.DeactivatePolicyAsync(policyId, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("policies/{policyId}/versions")]
    public async Task<IActionResult> CreatePolicyVersion(Guid policyId, [FromBody] CreateVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await _complianceService.CreatePolicyVersionAsync(policyId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("policies/{policyId}")]
    public async Task<IActionResult> GetPolicy(Guid policyId, CancellationToken cancellationToken)
    {
        var result = await _complianceService.GetPolicyAsync(policyId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("policies")]
    public async Task<IActionResult> GetPolicies([FromQuery] Guid? tenantId, [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var result = await _complianceService.GetPoliciesAsync(tenantId, includeInactive, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("consents")]
    public async Task<IActionResult> GiveConsent([FromBody] GiveConsentRequest request, CancellationToken cancellationToken)
    {
        var result = await _consentService.GiveConsentAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("consents/{consentId}/withdraw")]
    public async Task<IActionResult> WithdrawConsent(Guid consentId, [FromBody] WithdrawConsentRequest request, CancellationToken cancellationToken)
    {
        var result = await _consentService.WithdrawConsentAsync(consentId, request.Reason, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpGet("consents/users/{userId}")]
    public async Task<IActionResult> GetUserConsents(Guid userId, [FromQuery] Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var result = await _consentService.GetUserConsentsAsync(userId, tenantId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("consents/check")]
    public async Task<IActionResult> CheckConsent([FromQuery] Guid userId, [FromQuery] Guid policyId, CancellationToken cancellationToken)
    {
        var result = await _consentService.HasValidConsentAsync(userId, policyId, cancellationToken);
        return result.IsSuccess ? Ok(new { hasConsent = result.Value }) : BadRequest(result.Error);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLog([FromQuery] AuditLogRequest request, CancellationToken cancellationToken)
    {
        var result = await _complianceService.GetAuditLogAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("audit")]
    public async Task<IActionResult> RecordAudit([FromBody] RecordAuditRequest request, CancellationToken cancellationToken)
    {
        var result = await _consentService.RecordAuditAsync(request, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}

public record WithdrawConsentRequest(string? Reason);
