using Microsoft.AspNetCore.Authorization;
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
    private readonly ICqrsDispatcher _dispatcher;

    public ApiKeyController(ICqrsDispatcher dispatcher)
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
        return result.Match<IActionResult>(
            success => Ok(result.Value),
            failure => BadRequest(new { errors = result.Errors }));
    }

    /// <summary>
    ///     List all API keys for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListApiKeys(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new ListApiKeysQuery(), cancellationToken);
        return result.Match<IActionResult>(
            success => Ok(result.Value),
            failure => BadRequest(new { errors = result.Errors }));
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
        return result.Match<IActionResult>(
            success => Ok(new { message = "API key revoked successfully" }),
            failure => BadRequest(new { errors = result.Errors }));
    }
}

public record RevokeApiKeyRequest
{
    public string? Reason { get; init; }
}
