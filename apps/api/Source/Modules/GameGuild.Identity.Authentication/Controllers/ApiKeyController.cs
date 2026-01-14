using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Controller for API key management
/// </summary>
[ApiController]
[Route("api/auth/api-keys")]
[Authorize]
public class ApiKeyController : ControllerBase
{
    private readonly IMediator _dispatcher;

    public ApiKeyController(IMediator dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    ///     Create a new API key
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateApiKey(
        [FromBody] CreateApiKeyCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    ///     List all API keys for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApiKeys(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new ListApiKeysQuery(), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    ///     Revoke an API key
    /// </summary>
    [HttpPost("{keyId}/revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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

        var result = await _dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new { message = "API key revoked successfully" })
            : BadRequest(new { error = result.Error });
    }
}

public record RevokeApiKeyRequest
{
    public string? Reason { get; init; }
}
