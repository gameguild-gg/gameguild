using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Resources.Commands;
using GameGuild.Resources.Models;
using GameGuild.Resources.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Resources.Controllers;

/// <summary>
///     Controller for managing resource quotas
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants/{tenantId:guid}/quotas")]
public class QuotaController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Get all quotas for a tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResourceQuotaResponse>>> GetTenantQuotas(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var query = new GetTenantResourceQuotasQuery(tenantId);
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Get specific quota for a resource type
    /// </summary>
    [HttpGet("{type}")]
    public async Task<ActionResult<ResourceQuotaResponse>> GetQuota(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var query = new GetResourceQuotaQuery(tenantId, type);
        var result = await sender.Send(query, cancellationToken);

        if (result == null) return NotFound($"Quota not found for tenant {tenantId} and type {type}");

        return Ok(result);
    }

    /// <summary>
    ///     Set or update a quota
    /// </summary>
    [HttpPut("{type}")]
    public async Task<ActionResult> SetQuota(Guid tenantId, ResourceUsageType type, [FromBody] SetResourceQuotaRequest request, CancellationToken cancellationToken = default)
    {
        var command = new SetResourceQuotaCommand(tenantId, type, request.SoftLimit, request.HardLimit, request.Period, request.IsActive, request.ResetTime);

        await sender.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Reset quota usage to zero
    /// </summary>
    [HttpPost("{type}/reset")]
    public async Task<ActionResult> ResetQuota(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var command = new ResetResourceQuotaCommand(tenantId, type);
        await sender.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Delete a quota
    /// </summary>
    [HttpDelete("{type}")]
    public async Task<ActionResult> DeleteQuota(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var command = new DeleteResourceQuotaCommand(tenantId, type);
        await sender.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Activate or deactivate a quota
    /// </summary>
    [HttpPatch("{type}/toggle")]
    public async Task<ActionResult> ToggleQuota(Guid tenantId, ResourceUsageType type, [FromBody] ToggleResourceQuotaRequest request, CancellationToken cancellationToken = default)
    {
        var command = new ToggleResourceQuotaCommand(tenantId, type, request.IsActive);
        await sender.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///     Check quota enforcement for a specific amount
    /// </summary>
    [HttpPost("{type}/check")]
    public async Task<ActionResult<ResourceQuotaEnforcementResult>> CheckQuota(Guid tenantId, ResourceUsageType type, [FromBody] CheckResourceQuotaRequest request, CancellationToken cancellationToken = default)
    {
        var query = new CheckResourceQuotaQuery(tenantId, type, request.Amount);
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }
}
