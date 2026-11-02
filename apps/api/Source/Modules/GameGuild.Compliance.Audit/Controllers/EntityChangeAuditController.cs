using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Compliance.Audit;

/// <summary>
///     Universal entity-change audit surface (issue #166): the automatically captured change history
///     of every audited entity, served through the centralized audit module. Records are produced by
///     the EF change-tracking capture in every <c>SaveChanges</c> and redacted through the central
///     PII pipeline before persistence.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/audit/entity-changes")]
[Route("api/audit/entity-changes")]
[Tags("compliance/audit/entity-changes")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class EntityChangeAuditController(IEntityChangeAuditQueryService queryService) : ControllerBase
{
    /// <summary>
    ///     Returns the universal entity-change audit history for the current tenant. Tenant
    ///     administrators only; supports filtering by entity name, change kind, and actor.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(EntityChangeAuditListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EntityChangeAuditListResponse>> ListChanges(
        [FromQuery] EntityChangeAuditListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await queryService.ListForCurrentTenantAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (SecurityLogRetentionValidationException exception)
        {
            return BadRequest(new ValidationProblemDetails(exception.Errors.ToDictionary(static pair => pair.Key, static pair => pair.Value))
                { Status = StatusCodes.Status400BadRequest });
        }
    }
}
