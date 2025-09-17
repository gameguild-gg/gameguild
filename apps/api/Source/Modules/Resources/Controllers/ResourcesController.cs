using GameGuild.Modules.Resources.Models;
using GameGuild.Modules.Resources.Services;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Resources.Controllers;

/// <summary> API controller for resource quota management and usage tracking </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResourcesController : ControllerBase {
  private readonly IResourceQuotaService _resourceQuotaService;

  public ResourcesController(IResourceQuotaService resourceQuotaService) { _resourceQuotaService = resourceQuotaService; }

  /// <summary> Get resource usage overview for current tenant </summary>
  [HttpGet("usage")]
  public async Task<ActionResult<MultiResourceUsageResponse>> GetUsageOverview() {
    var tenantId = User.GetTenantId();
    var overview = await _resourceQuotaService.GetTenantUsageOverviewAsync(tenantId);

    return Ok(overview);
  }

  /// <summary> Get detailed usage information for a specific resource type </summary>
  [HttpGet("usage/{type}")]
  public async Task<ActionResult<ResourceUsageResponse>> GetResourceUsageDetails(ResourceUsageType type, [FromQuery] int historyDays = 30) {
    var tenantId = User.GetTenantId();
    var details = await _resourceQuotaService.GetResourceUsageDetailsAsync(tenantId, type, historyDays);

    return Ok(details);
  }

  /// <summary> Check if a resource usage request would exceed limits </summary>
  [HttpPost("check-limits")]
  public async Task<ActionResult<ResourceLimitCheckResponse>> CheckLimits([FromBody] CheckLimitsRequest request) {
    var tenantId = User.GetTenantId();
    var result = await _resourceQuotaService.CheckLimitsAsync(tenantId, request.Type, request.Amount);

    return Ok(result);
  }

  /// <summary> Check limits for multiple resource types </summary>
  [HttpPost("check-multiple-limits")]
  public async Task<ActionResult<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>>> CheckMultipleLimits([FromBody] Dictionary<ResourceUsageType, long> requestedAmounts) {
    var tenantId = User.GetTenantId();
    var results = await _resourceQuotaService.CheckMultipleLimitsAsync(tenantId, requestedAmounts);

    return Ok(results);
  }

  /// <summary> Attempt to consume resources if within limits </summary>
  [HttpPost("consume")]
  public async Task<ActionResult<ResourceLimitCheckResponse>> ConsumeResource([FromBody] ConsumeResourceRequest request) {
    var tenantId = User.GetTenantId();
    var userId = User.GetUserId();

    var result = await _resourceQuotaService.TryConsumeResourceAsync(tenantId, request.Type, request.Amount, userId, request.Source);

    return Ok(result);
  }

  /// <summary> Record resource usage (for batch operations) </summary>
  [HttpPost("record-usage")]
  public async Task<ActionResult<bool>> RecordUsage([FromBody] RecordUsageRequest request) {
    var tenantId = User.GetTenantId();
    var userId = User.GetUserId();

    var success = await _resourceQuotaService.RecordUsageAsync(tenantId, request.Type, request.Amount, userId, request.Source, request.Metadata);

    if (!success) return BadRequest("Failed to record usage");

    return Ok(success);
  }

  /// <summary> Get usage history for a resource type </summary>
  [HttpGet("usage/{type}/history")]
  public async Task<ActionResult<IEnumerable<ResourceUsageRecord>>> GetUsageHistory(ResourceUsageType type, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null) {
    var tenantId = User.GetTenantId();
    var history = await _resourceQuotaService.GetUsageHistoryAsync(tenantId, type, fromDate, toDate);

    return Ok(history);
  }

  // Admin endpoints (require appropriate permissions)

  /// <summary> Set resource quota for a tenant (admin only) </summary>
  [HttpPost("admin/quotas")]
  // [RequireRole("Admin", "TenantManager")]
  public async Task<ActionResult<ResourceQuota>> SetQuota([FromBody] SetQuotaRequest request) {
    var quota = await _resourceQuotaService.SetQuotaAsync(request.TenantId, request.Type, request.SoftLimit, request.HardLimit, request.Period);

    return Ok(quota);
  }

  /// <summary> Get all quotas for a tenant (admin only) </summary>
  [HttpGet("admin/tenants/{tenantId:guid}/quotas")]
  // [RequireRole("Admin", "TenantManager")]
  public async Task<ActionResult<IEnumerable<ResourceQuota>>> GetTenantQuotas(Guid tenantId) {
    var quotas = await _resourceQuotaService.GetTenantQuotasAsync(tenantId);

    return Ok(quotas);
  }

  /// <summary> Delete a resource quota (admin only) </summary>
  [HttpDelete("admin/quotas")]
  // [RequireRole("Admin", "TenantManager")]
  public async Task<ActionResult> DeleteQuota([FromBody] DeleteQuotaRequest request) {
    var success = await _resourceQuotaService.DeleteQuotaAsync(request.TenantId, request.Type);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Get tenants that have exceeded their limits (admin only) </summary>
  [HttpGet("admin/exceeding-limits")]
  // [RequireRole("Admin")]
  public async Task<ActionResult<IEnumerable<Guid>>> GetTenantsExceedingLimits([FromQuery] ResourceUsageType? type = null, [FromQuery] bool hardLimitOnly = false) {
    var tenants = await _resourceQuotaService.GetTenantsExceedingLimitsAsync(type, hardLimitOnly);

    return Ok(tenants);
  }

  /// <summary> Reset expired quotas (admin only) </summary>
  [HttpPost("admin/reset-expired-quotas")]
  // [RequireRole("Admin")]
  public async Task<ActionResult<int>> ResetExpiredQuotas() {
    var resetCount = await _resourceQuotaService.ResetExpiredQuotasAsync();

    return Ok(resetCount);
  }

  /// <summary> Clean up old usage records (admin only) </summary>
  [HttpDelete("admin/cleanup-usage-records")]
  // [RequireRole("Admin")]
  public async Task<ActionResult<int>> CleanupOldUsageRecords([FromQuery] DateTime olderThan) {
    var cleanedCount = await _resourceQuotaService.CleanupOldUsageRecordsAsync(olderThan);

    return Ok(cleanedCount);
  }

  /// <summary> Recalculate usage for a tenant and resource type (admin only) </summary>
  [HttpPost("admin/recalculate-usage")]
  // [RequireRole("Admin")]
  public async Task<ActionResult<bool>> RecalculateUsage([FromBody] RecalculateUsageRequest request) {
    var success = await _resourceQuotaService.RecalculateUsageAsync(request.TenantId, request.Type);

    if (!success) return BadRequest("Failed to recalculate usage");

    return Ok(success);
  }
}

// Request DTOs
public class CheckLimitsRequest {
  public ResourceUsageType Type { get; set; }

  public long Amount { get; set; } = 1;
}

public class ConsumeResourceRequest {
  public ResourceUsageType Type { get; set; }

  public long Amount { get; set; } = 1;

  public string? Source { get; set; }
}

public class RecordUsageRequest {
  public ResourceUsageType Type { get; set; }

  public long Amount { get; set; } = 1;

  public string? Source { get; set; }

  public Dictionary<string, string>? Metadata { get; set; }
}

public class SetQuotaRequest {
  public Guid TenantId { get; set; }

  public ResourceUsageType Type { get; set; }

  public long? SoftLimit { get; set; }

  public long? HardLimit { get; set; }

  public ResourceQuotaPeriod Period { get; set; } = ResourceQuotaPeriod.Monthly;
}

public class DeleteQuotaRequest {
  public Guid TenantId { get; set; }

  public ResourceUsageType Type { get; set; }
}

public class RecalculateUsageRequest {
  public Guid TenantId { get; set; }

  public ResourceUsageType Type { get; set; }
}
