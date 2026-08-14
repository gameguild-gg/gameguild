using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Users;

/// <summary>
///     Users API Controller - CRUD operations for individual users and user listing
/// </summary>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("users")]
[Authorize]
public sealed class UsersController(
    ISender sender,
    IActorContextAccessor actorContextAccessor,
    ITenantMembershipChecker tenantMembershipChecker) : BaseApiController
{
    #region Collection Operations - /v1/users

    /// <summary>
    ///     Create a new user
    /// </summary>
    /// <param name="body">User creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created user</returns>
    [HttpPost("v{version:apiVersion}/users")]
    [Authorize(Policy = Policies.UsersCreate)]
    [EndpointSummary("Create a new user")]
    [EndpointDescription("Creates a new user account with the provided information.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        UserDto user = await sender.Send(new CreateUserCommand(body.Email, body.Name, body.PhoneNumber), ct).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetUserById), new { userId = user.Id }, user);
    }

    /// <summary>
    ///     Get users with pagination, search, and sorting
    /// </summary>
    /// <param name="email">Filter by email (exact lookup)</param>
    /// <param name="status">Filter by status (active/inactive/deleted)</param>
    /// <param name="includeDeleted">Include soft-deleted users</param>
    /// <param name="q">Text search query</param>
    /// <param name="cursor">Cursor for pagination</param>
    /// <param name="limit">Number of items to return</param>
    /// <param name="sort">Sort field and direction (e.g., created_at, -created_at)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet("v{version:apiVersion}/users")]
    [EndpointSummary("Get users with pagination, search, and sorting")]
    [EndpointDescription("Retrieves a paginated list of users with optional filtering by email, status, and text search.")]
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? email = null,
        [FromQuery] string? status = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] string? q = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        [FromQuery] string? sort = null,
        CancellationToken ct = default
    )
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsTenantAdmin &&
            !actor.HasPermission(UsersPermission.Keys.Read) &&
            !actor.HasPermission(UsersPermission.Keys.Manage))
        {
            return Forbid();
        }

        var query = new GetUsersQuery(email, status, includeDeleted, q, cursor, limit, sort);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Individual Item Operations - /v1/users/{userId}

    /// <summary>
    ///     Check if user exists by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>200 if exists, 404 if not</returns>
    [HttpHead("v{version:apiVersion}/users/{userId:guid}")]
    [Authorize(Policy = Policies.UsersReadSelf)]
    [EndpointSummary("Check if user exists by ID")]
    [EndpointDescription("Checks if a user exists by ID without returning the body.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckUserExistsById(Guid userId, CancellationToken ct)
    {
        if (!await CanAccessUserAsync(userId, UsersPermission.Keys.Read, ct).ConfigureAwait(false))
        {
            return Forbid();
        }

        UserDto? user = await sender.Send(new GetUserByIdQuery(userId), ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok();
    }

    /// <summary>
    ///     Get user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User details</returns>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}")]
    [Authorize(Policy = Policies.UsersReadSelf)]
    [EndpointSummary("Get user by ID")]
    [EndpointDescription("Retrieves detailed information for a specific user by their unique identifier.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(Guid userId, CancellationToken ct)
    {
        if (!await CanAccessUserAsync(userId, UsersPermission.Keys.Read, ct).ConfigureAwait(false))
        {
            return Forbid();
        }

        UserDto? user = await sender.Send(new GetUserByIdQuery(userId), ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    ///     Partially update user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="body">Partial update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated user</returns>
    [HttpPatch("v{version:apiVersion}/users/{userId:guid}")]
    [Authorize(Policy = Policies.UsersEditSelf)]
    [EndpointSummary("Partially update user by ID")]
    [EndpointDescription("Updates specific fields of a user by ID.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchUserById(Guid userId, [FromBody] UpdateUserRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!await CanAccessUserAsync(userId, UsersPermission.Keys.Manage, ct).ConfigureAwait(false))
        {
            return Forbid();
        }

        var command = new UpdateUserCommand(userId, body.Name, body.PhoneNumber);
        var user = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(user);
    }

    /// <summary>
    ///     Update user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="body">Complete user data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated user</returns>
    [HttpPut("v{version:apiVersion}/users/{userId:guid}")]
    [Authorize(Policy = Policies.UsersEditSelf)]
    [EndpointSummary("Update user by ID")]
    [EndpointDescription("Fully updates a user by ID with complete user data.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserById(Guid userId, [FromBody] CreateUserRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!await CanAccessUserAsync(userId, UsersPermission.Keys.Manage, ct).ConfigureAwait(false))
        {
            return Forbid();
        }

        var command = new UpdateUserCommand(userId, body.Name, body.PhoneNumber);
        var user = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(user);
    }

    /// <summary>
    ///     Soft delete user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("v{version:apiVersion}/users/{userId:guid}")]
    [Authorize(Policy = Policies.UsersDeleteSelf)]
    [EndpointSummary("Soft delete user by ID")]
    [EndpointDescription("Soft deletes a user by ID (can be restored). Users can delete their own account.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserById(Guid userId, CancellationToken ct)
    {
        if (!await CanAccessUserAsync(userId, UsersPermission.Keys.Manage, ct).ConfigureAwait(false))
        {
            return Forbid();
        }

        await sender.Send(new DeleteUserCommand(userId), ct).ConfigureAwait(false);

        return NoContent();
    }

    private async Task<bool> CanAccessUserAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor.IsSystemAdmin || actor.SubjectIdAsGuid == userId)
        {
            return true;
        }

        if (!actor.TenantId.HasValue ||
            (!actor.IsTenantAdmin && !actor.HasPermission(permission) && !actor.HasPermission(UsersPermission.Keys.Manage)))
        {
            return false;
        }

        return await tenantMembershipChecker
            .IsUserMemberOfTenantAsync(userId, actor.TenantId.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion
}
