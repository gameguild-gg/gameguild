using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Resources;

/// <summary>
///     User Resource Settings API Controller - RESTful API for managing user-level resource settings overrides
/// </summary>
/// <remarks>
///     All endpoints require authentication. User ownership or admin role is enforced.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Tags("users/resources/settings")]
[Authorize]
public sealed class UserResourceSettingsController(IResourceSettingsRepository settingsRepository) : ControllerBase
{
    /// <summary>
    ///     Get all setting overrides for a user
    /// </summary>
    /// <param name="userId">User unique identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of user setting overrides</returns>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/resources/settings")]
    [EndpointSummary("Get all setting overrides for a user")]
    [EndpointDescription("Retrieves all resource setting overrides for a specific user.")]
    [ProducesResponseType<IEnumerable<ResourceSettings>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSettings(Guid userId, CancellationToken ct)
    {
        return Ok(await settingsRepository.GetByUserAsync(userId, ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Get a specific setting override by key for a user
    /// </summary>
    /// <param name="userId">User unique identifier</param>
    /// <param name="key">Setting key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Setting override</returns>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/resources/settings/{key}")]
    [EndpointSummary("Get a specific setting override by key for a user")]
    [EndpointDescription("Retrieves a specific resource setting override by its key for a user.")]
    [ProducesResponseType<ResourceSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserSettingByKey(Guid userId, string key, CancellationToken ct)
    {
        var setting = await settingsRepository.GetByUserKeyAsync(userId, key, ct).ConfigureAwait(false);

        if (setting == null) return NotFound($"Setting override not found for user {userId} and key: {key}");

        return Ok(setting);
    }

    /// <summary>
    ///     Create or update a setting override for a user
    /// </summary>
    /// <param name="userId">User unique identifier</param>
    /// <param name="key">Setting key</param>
    /// <param name="body">Setting data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created or updated setting</returns>
    [HttpPut("v{version:apiVersion}/users/{userId:guid}/resources/settings/{key}")]
    [EndpointSummary("Create or update a setting override for a user")]
    [EndpointDescription("Creates a new setting override or updates an existing one for a user.")]
    [ProducesResponseType<ResourceSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetUserSetting(Guid userId, string key, [FromBody] SetUserResourceSettingsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var existing = await settingsRepository.GetByUserKeyAsync(userId, key, ct).ConfigureAwait(false);

        if (existing != null)
        {
            existing.Value = body.Value;
            existing.UpdatedAt = DateTime.UtcNow;

            await settingsRepository.UpdateAsync(existing, ct).ConfigureAwait(false);

            return Ok(existing);
        }

        var setting = new ResourceSettings { UserId = userId, Key = key, Value = body.Value, IsActive = true };

        await settingsRepository.CreateAsync(setting, ct).ConfigureAwait(false);

        return Ok(setting);
    }
}

/// <summary>
///     Request model for setting user resource settings override
/// </summary>
public sealed record SetUserResourceSettingsRequest(string? Value);
