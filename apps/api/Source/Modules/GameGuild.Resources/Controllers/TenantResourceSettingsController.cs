using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Resources;

/// <summary>
///     Tenant Resource Settings API Controller - RESTful API for managing tenant-level resource settings
/// </summary>
/// <remarks>
///     All endpoints require authentication. Tenant membership validation is enforced.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Tags("tenants/resources/settings")]
[Authorize]
public sealed class TenantResourceSettingsController(IResourceSettingsRepository settingsRepository) : ControllerBase
{
    /// <summary>
    ///     Get all settings for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="category">Optional filter by category</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of settings</returns>
    [HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings")]
    [EndpointSummary("Get all settings for a tenant")]
    [EndpointDescription("Retrieves all resource settings for a specific tenant, optionally filtered by category.")]
    [ProducesResponseType<IEnumerable<ResourceSettings>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantSettings(Guid tenantId, [FromQuery] string? category, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(category)) { return Ok(await settingsRepository.GetByCategoryAsync(tenantId, category, ct).ConfigureAwait(false)); }

        return Ok(await settingsRepository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Get a specific setting by key
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="key">Setting key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Setting entry</returns>
    [HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings/{key}")]
    [EndpointSummary("Get a specific setting by key")]
    [EndpointDescription("Retrieves a specific resource setting by its key for a tenant.")]
    [ProducesResponseType<ResourceSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantSettingByKey(Guid tenantId, string key, CancellationToken ct)
    {
        var setting = await settingsRepository.GetByKeyAsync(tenantId, key, ct).ConfigureAwait(false);

        if (setting == null) return NotFound($"Setting not found for key: {key}");

        return Ok(setting);
    }

    /// <summary>
    ///     Get effective value for a setting (considering user overrides)
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="key">Setting key</param>
    /// <param name="userId">Optional user ID for user-level override lookup</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Effective value</returns>
    [HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings/{key}/effective")]
    [EndpointSummary("Get effective value for a setting")]
    [EndpointDescription("Retrieves the effective value for a setting, considering user-level overrides if a user ID is provided.")]
    [ProducesResponseType<EffectiveSettingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEffectiveValue(Guid tenantId, string key, [FromQuery] Guid? userId, CancellationToken ct)
    {
        var value = await settingsRepository.GetEffectiveValueAsync(tenantId, key, userId, ct).ConfigureAwait(false);

        if (value == null) return NotFound($"Setting not found for key: {key}");

        return Ok(new EffectiveSettingResponse(key, value, userId.HasValue));
    }

    /// <summary>
    ///     Create or update a setting
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="key">Setting key</param>
    /// <param name="body">Setting data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created or updated setting</returns>
    [HttpPut("v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings/{key}")]
    [EndpointSummary("Create or update a setting")]
    [EndpointDescription("Creates a new setting or updates an existing one for a tenant.")]
    [ProducesResponseType<ResourceSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetTenantSetting(Guid tenantId, string key, [FromBody] SetResourceSettingsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var existing = await settingsRepository.GetByKeyAsync(tenantId, key, ct).ConfigureAwait(false);

        if (existing != null)
        {
            existing.Value = body.Value;
            existing.DefaultValue = body.DefaultValue ?? existing.DefaultValue;
            existing.DataType = body.DataType ?? existing.DataType;
            existing.Description = body.Description ?? existing.Description;
            existing.Category = body.Category ?? existing.Category;
            existing.AllowUserOverride = body.AllowUserOverride ?? existing.AllowUserOverride;
            existing.DisplayOrder = body.DisplayOrder ?? existing.DisplayOrder;
            existing.ValidationRules = body.ValidationRules ?? existing.ValidationRules;
            existing.UpdatedAt = DateTime.UtcNow;

            await settingsRepository.UpdateAsync(existing, ct).ConfigureAwait(false);

            return Ok(existing);
        }

        var setting = new ResourceSettings
        {
            Key = key,
            Value = body.Value,
            DefaultValue = body.DefaultValue,
            DataType = body.DataType ?? "String",
            Description = body.Description,
            Category = body.Category,
            AllowUserOverride = body.AllowUserOverride ?? true,
            DisplayOrder = body.DisplayOrder ?? 0,
            ValidationRules = body.ValidationRules,
            IsActive = true
        };

        // Set TenantId using reflection since the setter is protected
        var tenantIdProperty = typeof(ResourceSettings).GetProperty("TenantId");
        tenantIdProperty?.GetSetMethod(nonPublic: true)?.Invoke(setting, new object[] { tenantId });

        await settingsRepository.CreateAsync(setting, ct).ConfigureAwait(false);

        return Ok(setting);
    }

    /// <summary>
    ///     Delete a setting
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="key">Setting key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings/{key}")]
    [EndpointSummary("Delete a setting")]
    [EndpointDescription("Removes a resource setting for a tenant.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenantSetting(Guid tenantId, string key, CancellationToken ct)
    {
        var deleted = await settingsRepository.DeleteByKeyAsync(tenantId, key, ct).ConfigureAwait(false);

        if (!deleted) return NotFound($"Setting not found for key: {key}");

        return NoContent();
    }
}

/// <summary>
///     Request model for setting resource settings (tenant level)
/// </summary>
public sealed record SetResourceSettingsRequest(
    string? Value,
    string? DefaultValue = null,
    string? DataType = null,
    string? Description = null,
    string? Category = null,
    bool? AllowUserOverride = null,
    int? DisplayOrder = null,
    string? ValidationRules = null
);

/// <summary>
///     Response model for effective setting value
/// </summary>
public sealed record EffectiveSettingResponse(string Key, string Value, bool IsUserOverride);
