using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Certificates;

/// <summary>
/// Controller for certificate management and verification
/// </summary>
[Route("api/certificates")]
[Authorize]
public class CertificatesController : BaseApiController
{
    private readonly ICertificateService _certificateService;
    private readonly ICertificateTemplateService _templateService;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly ILogger<CertificatesController> _logger;

    public CertificatesController(
        ICertificateService certificateService,
        ICertificateTemplateService templateService,
        IActorContextAccessor actorContextAccessor,
        ILogger<CertificatesController> logger)
    {
        _certificateService = certificateService;
        _templateService = templateService;
        _actorContextAccessor = actorContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Get certificates for the current user
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<CertificateDto>>> GetMyCertificates()
    {
        var actor = _actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid == null)
        {
            return Unauthorized();
        }

        var certificates = await _certificateService.GetUserCertificatesAsync(actor.SubjectIdAsGuid.Value, actor.TenantId);
        var dtos = certificates.Select(c => CertificateDto.FromEntity(c));

        return Ok(dtos);
    }

    /// <summary>
    /// Get a certificate by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CertificateDto>> GetCertificate(Guid id)
    {
        var certificate = await _certificateService.GetCertificateByIdAsync(id);
        if (certificate == null)
        {
            return NotFound();
        }

        return Ok(CertificateDto.FromEntity(certificate));
    }

    /// <summary>
    /// Verify a certificate by its number (public endpoint)
    /// </summary>
    [HttpGet("verify/{certificateNumber}")]
    [AllowAnonymous]
    public async Task<ActionResult<CertificateVerificationResult>> VerifyCertificate(string certificateNumber)
    {
        var result = await _certificateService.VerifyCertificateAsync(certificateNumber);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Issue a certificate for an enrollment
    /// </summary>
    [HttpPost("issue")]
    [RequireResourcePermission<PermissionType, Certificate>(PermissionType.Create)]
    public async Task<ActionResult<CertificateDto>> IssueCertificate([FromBody] IssueCertificateRequest request)
    {
        var actor = _actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid == null)
        {
            return Unauthorized();
        }

        var result = await _certificateService.IssueCertificateAsync(
            request.TemplateId,
            request.EnrollmentId,
            request.UserId,
            request.CourseId,
            actor.TenantId);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetCertificate), new { id = result.Value.Id }, CertificateDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Revoke a certificate
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    [RequireResourcePermission<PermissionType, Certificate>(PermissionType.Delete)]
    public async Task<ActionResult> RevokeCertificate(Guid id, [FromBody] RevokeCertificateRequest request)
    {
        var result = await _certificateService.RevokeCertificateAsync(id, request.Reason);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Get certificates for a specific course
    /// </summary>
    [HttpGet("course/{courseId:guid}")]
    public async Task<ActionResult<IEnumerable<CertificateDto>>> GetCourseCertificates(Guid courseId)
    {
        var actor = _actorContextAccessor.ActorContext;

        var certificates = await _certificateService.GetCourseCertificatesAsync(courseId, actor.TenantId);
        var dtos = certificates.Select(c => CertificateDto.FromEntity(c));

        return Ok(dtos);
    }

    /// <summary>
    /// Get certificates expiring within the specified days
    /// </summary>
    [HttpGet("expiring")]
    [RequireContentTypePermission<Certificate>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<CertificateDto>>> GetExpiringCertificates([FromQuery] int days = 30)
    {
        var certificates = await _certificateService.GetExpiringCertificatesAsync(days);
        var dtos = certificates.Select(c => CertificateDto.FromEntity(c));

        return Ok(dtos);
    }
}

/// <summary>
/// DTO for certificate display
/// </summary>
public sealed record CertificateDto(
    Guid Id,
    string CertificateNumber,
    string RecipientName,
    string CourseName,
    DateTime IssuedAt,
    DateTime? ExpiresAt,
    CertificateStatus Status)
{
    public static CertificateDto FromEntity(Certificate c) => new(
        c.Id,
        c.CertificateNumber,
        c.RecipientName,
        c.CourseName,
        c.IssuedAt,
        c.ExpiresAt,
        c.Status);
}

/// <summary>
/// Request to issue a certificate
/// </summary>
public sealed record IssueCertificateRequest(
    Guid TemplateId,
    Guid EnrollmentId,
    Guid UserId,
    Guid CourseId);

/// <summary>
/// Request to revoke a certificate
/// </summary>
public sealed record RevokeCertificateRequest(string Reason);
