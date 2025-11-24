using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Users.Commands;
using GameGuild.Users.Models;
using GameGuild.Users.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Users.Controllers;

/// <summary>
///     Controller for managing user metadata
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "user-metadata")]
[Tags("user-metadata")]
public sealed class UserMetadataController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Get user metadata by user ID
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/metadata")]
    [EndpointSummary("Get user metadata by user ID")]
    [ProducesResponseType<UserMetadataDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetadata(Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new GetUserMetadataQuery(userId), ct).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    ///     Partially update user metadata by user ID
    /// </summary>
    [HttpPatch("v{version:apiVersion}/users/{userId:guid}/metadata")]
    [EndpointSummary("Partially update user metadata by user ID")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMetadata(Guid userId, [FromBody] UpdateUserMetadataRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateUserMetadataCommand(userId, body), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Replace user metadata by user ID
    /// </summary>
    [HttpPut("v{version:apiVersion}/users/{userId:guid}/metadata")]
    [EndpointSummary("Replace user metadata by user ID")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReplaceMetadata(Guid userId, [FromBody] ReplaceUserMetadataRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new ReplaceUserMetadataCommand(userId, body), ct).ConfigureAwait(false);
        return NoContent();
    }
}
