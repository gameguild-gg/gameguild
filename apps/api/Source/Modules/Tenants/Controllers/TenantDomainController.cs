using GameGuild.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// REST API controller for tenant domain management using service-first pattern
/// Provides endpoints for managing tenant domains, domain validation, and tenant-domain associations
/// </summary>
[ApiController]
[Route("api/tenant-domains")]
[Authorize]
public class TenantDomainController(ITenantDomainsService tenantDomainsService) : ControllerBase
{
    /// <summary> Get all domains for a specific tenant </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> List of tenant domains </returns>
    [HttpGet("tenant/{tenantId:guid}")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<IReadOnlyList<TenantDomain>>> GetTenantDomains(Guid tenantId, CancellationToken cancellationToken)
    {
        var domains = await tenantDomainsService.GetTenantDomainsAsync(tenantId, cancellationToken);
        return Ok(domains);
    }

    /// <summary> Get a specific tenant domain by ID </summary>
    /// <param name="domainId"> Domain ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Tenant domain details </returns>
    [HttpGet("{domainId:guid}")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<TenantDomain>> GetTenantDomainById(Guid domainId, CancellationToken cancellationToken)
    {
        var domain = await tenantDomainsService.GetTenantDomainByIdAsync(domainId, cancellationToken);

        if (domain == null) return NotFound(new { Message = $"Tenant domain with ID {domainId} not found." });

        return Ok(domain);
    }

    /// <summary> Get all tenant domains (administrative) </summary>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> List of all tenant domains </returns>
    [HttpGet]
    [RequireSystemAdmin]
    public async Task<ActionResult<IReadOnlyList<TenantDomain>>> GetAllTenantDomains(CancellationToken cancellationToken)
    {
        var domains = await tenantDomainsService.GetAllTenantDomainsAsync(cancellationToken);
        return Ok(domains);
    }

    /// <summary> Get the primary domain for a tenant </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Primary tenant domain </returns>
    [HttpGet("tenant/{tenantId:guid}/primary")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<TenantDomain>> GetPrimaryDomain(Guid tenantId, CancellationToken cancellationToken)
    {
        var domain = await tenantDomainsService.GetPrimaryTenantDomainAsync(tenantId, cancellationToken);

        if (domain == null) return NotFound(new { Message = $"No primary domain found for tenant {tenantId}." });

        return Ok(domain);
    }

    /// <summary> Find tenant by domain </summary>
    /// <param name="topLevelDomain"> Top-level domain </param>
    /// <param name="subdomain"> Optional subdomain </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Tenant associated with the domain </returns>
    [HttpGet("find-tenant")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<Tenant>> FindTenantByDomain([FromQuery] string topLevelDomain, [FromQuery] string? subdomain = null, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantDomainsService.FindTenantByDomainAsync(topLevelDomain, subdomain, cancellationToken);

        if (tenant == null) return NotFound(new { Message = $"No tenant found for domain {(string.IsNullOrEmpty(subdomain) ? topLevelDomain : $"{subdomain}.{topLevelDomain}")}." });

        return Ok(tenant);
    }

    /// <summary> Find tenant domain by domain match </summary>
    /// <param name="topLevelDomain"> Top-level domain </param>
    /// <param name="subdomain"> Optional subdomain </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Matching tenant domain </returns>
    [HttpGet("find")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<TenantDomain>> FindTenantDomainByMatch([FromQuery] string topLevelDomain, [FromQuery] string? subdomain = null, CancellationToken cancellationToken = default)
    {
        var domain = await tenantDomainsService.FindTenantDomainByMatchAsync(topLevelDomain, subdomain, cancellationToken);

        if (domain == null) return NotFound(new { Message = $"No domain found matching {(string.IsNullOrEmpty(subdomain) ? topLevelDomain : $"{subdomain}.{topLevelDomain}")}." });

        return Ok(domain);
    }

    /// <summary> Create a new tenant domain </summary>
    /// <param name="request"> Domain creation request </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Created tenant domain </returns>
    [HttpPost]
    [RequireTenantPermission(PermissionType.Create)]
    public async Task<ActionResult<TenantDomain>> CreateTenantDomain([FromBody] CreateTenantDomainRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var domain = await tenantDomainsService.CreateTenantDomainAsync(
            request.TenantId,
            request.TopLevelDomain,
            request.Subdomain,
            request.IsMainDomain,
            cancellationToken
        );

        return CreatedAtAction(nameof(GetTenantDomainById), new { domainId = domain.Id }, domain);
    }

    /// <summary> Update an existing tenant domain </summary>
    /// <param name="domainId"> Domain ID </param>
    /// <param name="request"> Domain update request </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Updated tenant domain </returns>
    [HttpPut("{domainId:guid}")]
    [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult<TenantDomain>> UpdateTenantDomain(Guid domainId, [FromBody] UpdateTenantDomainRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var domain = await tenantDomainsService.UpdateTenantDomainAsync(
            domainId,
            request.TopLevelDomain,
            request.Subdomain,
            request.IsMainDomain,
            cancellationToken
        );

        return Ok(domain);
    }

    /// <summary> Delete a tenant domain </summary>
    /// <param name="domainId"> Domain ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Success status </returns>
    [HttpDelete("{domainId:guid}")]
    [RequireTenantPermission(PermissionType.Delete)]
    public async Task<IActionResult> DeleteTenantDomain(Guid domainId, CancellationToken cancellationToken)
    {
        var success = await tenantDomainsService.DeleteTenantDomainAsync(domainId, cancellationToken);

        if (!success) return NotFound(new { Message = $"Tenant domain with ID {domainId} not found." });

        return NoContent();
    }

    /// <summary> Set a domain as the primary domain for a tenant </summary>
    /// <param name="domainId"> Domain ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Updated tenant domain </returns>
    [HttpPost("{domainId:guid}/set-primary")]
    [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult<TenantDomain>> SetPrimaryDomain(Guid domainId, CancellationToken cancellationToken)
    {
        var domain = await tenantDomainsService.SetPrimaryDomainAsync(domainId, cancellationToken);
        return Ok(domain);
    }

    /// <summary> Validate domain format and availability </summary>
    /// <param name="topLevelDomain"> Top-level domain </param>
    /// <param name="subdomain"> Optional subdomain </param>
    /// <param name="excludeDomainId"> Optional domain ID to exclude from availability check </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Validation result </returns>
    [HttpGet("validate")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<DomainValidationResult>> ValidateDomain(
        [FromQuery] string topLevelDomain,
        [FromQuery] string? subdomain = null,
        [FromQuery] Guid? excludeDomainId = null,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await tenantDomainsService.ValidateDomainAsync(topLevelDomain, subdomain, excludeDomainId, cancellationToken);
        return Ok(validationResult);
    }

    /// <summary> Check if a domain combination is available </summary>
    /// <param name="topLevelDomain"> Top-level domain </param>
    /// <param name="subdomain"> Optional subdomain </param>
    /// <param name="excludeDomainId"> Optional domain ID to exclude from the check </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Availability status </returns>
    [HttpGet("check-availability")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<bool>> CheckDomainAvailability(
        [FromQuery] string topLevelDomain,
        [FromQuery] string? subdomain = null,
        [FromQuery] Guid? excludeDomainId = null,
        CancellationToken cancellationToken = default)
    {
        var isAvailable = await tenantDomainsService.IsDomainAvailableAsync(topLevelDomain, subdomain, excludeDomainId, cancellationToken);
        return Ok(new { IsAvailable = isAvailable });
    }
}

/// <summary> Request model for creating a tenant domain </summary>
public class CreateTenantDomainRequest
{
    /// <summary> The tenant ID </summary>
    public required Guid TenantId { get; set; }

    /// <summary> The top-level domain </summary>
    public required string TopLevelDomain { get; set; }

    /// <summary> Optional subdomain </summary>
    public string? Subdomain { get; set; }

    /// <summary> Whether this is the main domain </summary>
    public bool IsMainDomain { get; set; } = false;
}

/// <summary> Request model for updating a tenant domain </summary>
public class UpdateTenantDomainRequest
{
    /// <summary> The top-level domain </summary>
    public required string TopLevelDomain { get; set; }

    /// <summary> Optional subdomain </summary>
    public string? Subdomain { get; set; }

    /// <summary> Whether this is the main domain </summary>
    public bool IsMainDomain { get; set; } = false;
}
