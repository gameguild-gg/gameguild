using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Users;

/// <summary>
///     Controller for managing user metadata
/// </summary>
[ApiVersion("1.0")]
[Tags("users/metadata")]
[Authorize]
public sealed class UserMetadataController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Get user metadata by user ID
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/metadata")]
    [Authorize(Policy = Policies.UsersReadSelf)]
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
    [Authorize(Policy = Policies.UsersEditSelf)]
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
    [Authorize(Policy = Policies.UsersEditSelf)]
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
