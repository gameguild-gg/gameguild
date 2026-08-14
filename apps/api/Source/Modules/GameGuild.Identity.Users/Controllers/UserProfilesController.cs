using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Users;

/// <summary>
///     Controller for managing user profiles and social links
/// </summary>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("users/profiles")]
[Authorize]
public sealed class UserProfilesController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Find all user profiles with pagination, search, and sorting
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/profiles")]
    [Authorize(Policy = Policies.UsersRead)]
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
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get user profile by user ID
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/profile")]
    [Authorize(Policy = Policies.UsersReadSelf)]
    [EndpointSummary("Get user profile by user ID")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken ct)
    {
        var query = new GetUserProfileQuery(userId);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    ///     Update user profile (partial update)
    /// </summary>
    [HttpPatch("v{version:apiVersion}/users/{userId:guid}/profile")]
    [Authorize(Policy = Policies.UsersEditSelf)]
    [EndpointSummary("Update user profile (partial update)")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile(Guid userId, [FromBody] UpdateUserProfileRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var command = new UpdateUserProfileCommand(userId, body);
        var profile = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(profile);
    }

    /// <summary>
    ///     Replace user profile (full update)
    /// </summary>
    [HttpPut("v{version:apiVersion}/users/{userId:guid}/profile")]
    [Authorize(Policy = Policies.UsersEditSelf)]
    [EndpointSummary("Replace user profile (full update)")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReplaceProfile(Guid userId, [FromBody] ReplaceUserProfileRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var command = new ReplaceUserProfileCommand(userId, body);
        var profile = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(profile);
    }
}
