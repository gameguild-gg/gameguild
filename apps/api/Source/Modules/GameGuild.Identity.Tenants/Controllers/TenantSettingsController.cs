using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Controller for managing tenant settings.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenants/{tenantId:guid}/settings")]
[Microsoft.AspNetCore.Http.Tags("tenants/settings")]
[Authorize]
public sealed class TenantSettingsController(ISender sender) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Get tenant settings by tenant ID")]
    [EndpointDescription("Retrieves comprehensive tenant settings including system configuration, feature toggles, business rules, and operational preferences.")]
    [ProducesResponseType<TenantSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSettings(Guid tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantSettingsQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPatch]
    [EndpointSummary("Partially update tenant settings by tenant ID")]
    [EndpointDescription("Updates specific tenant settings fields without affecting other settings. Only the provided settings are modified.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSettings(Guid tenantId, [FromBody] UpdateTenantSettingsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateTenantSettingsCommand(tenantId, body), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPut]
    [EndpointSummary("Replace all tenant settings by tenant ID")]
    [EndpointDescription("Replaces all tenant settings with new values. All existing settings are replaced with the provided data.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReplaceSettings(Guid tenantId, [FromBody] ReplaceTenantSettingsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new ReplaceTenantSettingsCommand(tenantId, body), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("feature-flags")]
    [EndpointSummary("Get tenant feature flags")]
    [EndpointDescription("Retrieves all feature flags configured for the tenant for experimental features and A/B testing.")]
    [ProducesResponseType<Dictionary<string, bool>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFeatureFlags(Guid tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantFeatureFlagsQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPatch("feature-flags")]
    [EndpointSummary("Update tenant feature flags")]
    [EndpointDescription("Updates specific feature flags for the tenant. Existing flags not specified are preserved.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateFeatureFlags(Guid tenantId, [FromBody] Dictionary<string, bool> featureFlags, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(featureFlags);
        await sender.Send(new UpdateTenantFeatureFlagsCommand(tenantId, new UpdateTenantFeatureFlagsRequest(featureFlags)), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("system-limits")]
    [EndpointSummary("Get tenant system limits")]
    [EndpointDescription("Retrieves system limits and resource constraints configured for the tenant.")]
    [ProducesResponseType<TenantSystemLimitsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSystemLimits(Guid tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantSystemLimitsQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPatch("system-limits")]
    [EndpointSummary("Update tenant system limits")]
    [EndpointDescription("Updates system limits and resource constraints for the tenant.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSystemLimits(Guid tenantId, [FromBody] UpdateTenantSystemLimitsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateTenantSystemLimitsCommand(tenantId, body), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("integration-settings")]
    [EndpointSummary("Get tenant integration settings")]
    [EndpointDescription("Retrieves third-party integration configurations for the tenant.")]
    [ProducesResponseType<TenantIntegrationSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetIntegrationSettings(Guid tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantIntegrationSettingsQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPatch("integration-settings")]
    [EndpointSummary("Update tenant integration settings")]
    [EndpointDescription("Updates third-party integration configurations for the tenant.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateIntegrationSettings(Guid tenantId, [FromBody] UpdateTenantIntegrationSettingsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateTenantIntegrationSettingsCommand(tenantId, body), ct).ConfigureAwait(false);
        return NoContent();
    }
}
