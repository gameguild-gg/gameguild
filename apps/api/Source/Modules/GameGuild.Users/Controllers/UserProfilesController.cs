using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Users.Commands;
using GameGuild.Users.Models;
using GameGuild.Users.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Users.Controllers;

/// <summary>
///     Controller for managing user profiles and social links
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "user-profiles")]
[Tags("user-profiles")]
public sealed class UserProfilesController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Find all user profiles with pagination, search, and sorting
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/profiles")]
    [EndpointSummary("Find all user profiles with pagination, search, and sorting")]
    [ProducesResponseType<PagedResult<UserProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProfiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken ct = default
    )
    {
        var query = new GetUserProfilesPagedQuery(search, sortBy, sortDirection, page, pageSize);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }

    /// <summary>
    ///     Get user profile by user ID
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/profile")]
    [EndpointSummary("Get user profile by user ID")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken ct)
    {
        var query = new GetUserProfileQuery(userId);
        var result = await sender.Send(query, ct);

        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    ///     Update user profile (partial update)
    /// </summary>
    [HttpPatch("v{version:apiVersion}/users/{userId:guid}/profile")]
    [EndpointSummary("Update user profile (partial update)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile(Guid userId, [FromBody] UpdateUserProfileRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var command = new UpdateUserProfileCommand(userId, body);
        await sender.Send(command, ct);

        return NoContent();
    }

    /// <summary>
    ///     Replace user profile (full update)
    /// </summary>
    [HttpPut("v{version:apiVersion}/users/{userId:guid}/profile")]
    [EndpointSummary("Replace user profile (full update)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReplaceProfile(Guid userId, [FromBody] ReplaceUserProfileRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var command = new ReplaceUserProfileCommand(userId, body);
        await sender.Send(command, ct);

        return NoContent();
    }
}
