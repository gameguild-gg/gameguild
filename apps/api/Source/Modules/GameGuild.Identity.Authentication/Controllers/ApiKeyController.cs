using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Controller for API key management
/// </summary>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("auth/api-keys")]
[Authorize]
public class ApiKeyController : BaseApiController
{
    private readonly ISender _dispatcher;
    private readonly IStepUpRequirementService _stepUpRequirements;

    public ApiKeyController(ISender dispatcher, IStepUpRequirementService stepUpRequirements)
    {
        _dispatcher = dispatcher;
        _stepUpRequirements = stepUpRequirements;
    }

    /// <summary>
    ///     Create a new API key after fresh verification
    /// </summary>
    [HttpPost("v{version:apiVersion}/auth/api-keys")]
    [Authorize(Policy = ApiKeyScopePolicies.Prefix + ApiKeyScopes.ManageApiKeys)]
    [Authorize(Policy = TrustedDevicePolicy.Name)]
    [EndpointSummary("Create a new API key")]
    [EndpointDescription("Creates an API key after fresh verification. Without a stepUpReceipt, a valid request returns HTTP 403 with a persisted challenge bound to the name, scopes, expiry and IP whitelist. Verify an enrolled supported factor and retry the identical creation fields with the returned receipt. Invalid or reused receipts cannot create a key.")]
    [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(StepUpAuthenticationRequiredResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateApiKey(
        [FromBody] CreateApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var intent = command.Snapshot();
        var result = await _dispatcher.Send(intent, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure && result.Error.Code == "StepUp.Required")
        {
            try
            {
                var requirement = await _stepUpRequirements.CreateAsync(
                    CreateApiKeyHandler.StepUpBinding(intent), cancellationToken).ConfigureAwait(false);
                return StatusCode(StatusCodes.Status403Forbidden, requirement);
            }
            catch (StepUpContextUnavailableException)
            {
                return Unauthorized(new { error = "An authenticated tenant and session context is required" });
            }
        }

        if (result.IsFailure && result.Error.Code is "Auth.Required" or "Auth.ContextUnavailable")
        {
            return Unauthorized(new { error = result.Error });
        }

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    ///     List all API keys for the current user
    /// </summary>
    [HttpGet("v{version:apiVersion}/auth/api-keys")]
    [Authorize(Policy = ApiKeyScopePolicies.Prefix + ApiKeyScopes.ManageApiKeys)]
    [EndpointSummary("List all API keys")]
    [ProducesResponseType(typeof(List<ApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApiKeys(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new ListApiKeysQuery(), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    ///     Rotate an API key: issues a replacement key and starts the old key's
    ///     overlap (grace) window, after which the old key is revoked.
    /// </summary>
    [HttpPost("v{version:apiVersion}/auth/api-keys/{keyId}:rotate")]
    [Authorize(Policy = ApiKeyScopePolicies.Prefix + ApiKeyScopes.ManageApiKeys)]
    [Authorize(Policy = TrustedDevicePolicy.Name)]
    [EndpointSummary("Rotate an API key")]
    [ProducesResponseType(typeof(RotateApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateApiKey(
        Guid keyId,
        [FromBody] RotateApiKeyRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new RotateApiKeyCommand
        {
            KeyId = keyId,
            Name = request?.Name,
            Scopes = request?.Scopes,
            ExpiresAt = request?.ExpiresAt,
            GracePeriod = request?.GracePeriodMinutes is { } graceMinutes ? TimeSpan.FromMinutes(graceMinutes) : null
        };

        var result = await _dispatcher.Send(command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    ///     Revoke an API key
    /// </summary>
    [HttpPost("v{version:apiVersion}/auth/api-keys/{keyId}:revoke")]
    [Authorize(Policy = ApiKeyScopePolicies.Prefix + ApiKeyScopes.ManageApiKeys)]
    [Authorize(Policy = TrustedDevicePolicy.Name)]
    [EndpointSummary("Revoke an API key")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(
        Guid keyId,
        [FromBody] RevokeApiKeyRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new RevokeApiKeyCommand
        {
            KeyId = keyId,
            Reason = request?.Reason
        };

        var result = await _dispatcher.Send(command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Ok(new { message = "API key revoked successfully" })
            : BadRequest(new { error = result.Error });
    }
}

public sealed record RevokeApiKeyRequest
{
    public string? Reason { get; init; }
}

public sealed record RotateApiKeyRequest
{
    /// <summary>
    ///     Optional display name for the replacement key; defaults to the old key's name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     Optional scopes for the replacement key; defaults to the old key's scopes.
    /// </summary>
    public string[]? Scopes { get; init; }

    /// <summary>
    ///     Optional absolute expiry for the replacement key; defaults to preserving the old key's expiry.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    ///     Optional overlap window, in minutes, during which the old key keeps working;
    ///     defaults to the configured rotation grace period. Zero revokes the old key immediately.
    /// </summary>
    public int? GracePeriodMinutes { get; init; }
}
