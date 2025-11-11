using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Users.Commands;
using GameGuild.Users.Models;
using GameGuild.Users.Queries;
using GameGuild.Users.RequestModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Users;

/// <summary>
///     Users API Controller - RESTful API for user management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "users")]
[Tags("users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    #region Collection Operations - /v1/users

    /// <summary>
    ///     Create a new user
    /// </summary>
    /// <param name="body">User creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created user</returns>
    [HttpPost("v{version:apiVersion}/users")]
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
        var query = new GetUsersQuery(email, status, includeDeleted, q, cursor, limit, sort);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Bulk Operations - /v1/users:action

    /// <summary>
    ///     Bulk create users
    /// </summary>
    /// <param name="request">Bulk create request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created user data</returns>
    [HttpPost("v{version:apiVersion}/users:create")]
    [EndpointSummary("Bulk create users")]
    [EndpointDescription("Creates multiple user accounts at once.")]
    [ProducesResponseType<BulkCreateUsersResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreateUsers([FromBody] BulkCreateUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        BulkCreateUsersResult result = await sender.Send(new BulkCreateUsersCommand(request.Users), ct).ConfigureAwait(false);

        return Created(string.Empty, result);
    }

    /// <summary>
    ///     Bulk partial update users
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/users:update")]
    [EndpointSummary("Bulk partial update users")]
    [EndpointDescription("Updates multiple users with partial data.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkPartialUpdateUsers([FromBody] BulkUpdateUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await sender.Send(new BulkUpdateUsersCommand(request.Updates), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Bulk full update users
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/users:replace")]
    [EndpointSummary("Bulk full update users")]
    [EndpointDescription("Updates multiple users with complete data.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkFullUpdateUsers([FromBody] BulkUpdateUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await sender.Send(new BulkUpdateUsersCommand(request.Updates), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Bulk soft delete users
    /// </summary>
    /// <param name="request">Bulk delete request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/users:delete")]
    [EndpointSummary("Bulk soft delete users")]
    [EndpointDescription("Soft deletes multiple users at once.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteUsers([FromBody] BulkDeleteUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await sender.Send(new BulkDeleteUsersCommand(request.UserIds), ct).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    ///     Bulk activate user accounts
    /// </summary>
    /// <param name="request">Bulk activate request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Activated user data</returns>
    [HttpPost("v{version:apiVersion}/users:activate")]
    [EndpointSummary("Bulk activate user accounts")]
    [EndpointDescription("Activates multiple user accounts at once.")]
    [ProducesResponseType<BulkActivateUsersResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkActivateUsers([FromBody] BulkActivateUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        BulkActivateUsersResult result = await sender.Send(new BulkActivateUsersCommand(request.UserIds), ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk deactivate user accounts
    /// </summary>
    /// <param name="request">Bulk deactivate request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Deactivated user data</returns>
    [HttpPost("v{version:apiVersion}/users:deactivate")]
    [EndpointSummary("Bulk deactivate user accounts")]
    [EndpointDescription("Deactivates multiple user accounts at once.")]
    [ProducesResponseType<BulkDeactivateUsersResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeactivateUsers([FromBody] BulkDeactivateUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        BulkDeactivateUsersResult result = await sender.Send(new BulkDeactivateUsersCommand(request.UserIds), ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk suspend user accounts
    /// </summary>
    /// <param name="request">Bulk suspend request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Suspended user data</returns>
    [HttpPost("v{version:apiVersion}/users:suspend")]
    [EndpointSummary("Bulk suspend user accounts")]
    [EndpointDescription("Suspends multiple user accounts at once.")]
    [ProducesResponseType<BulkSuspendUsersResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkSuspendUsers([FromBody] BulkSuspendUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        BulkSuspendUsersResult result = await sender.Send(new BulkSuspendUsersCommand(request.UserIds), ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk unsuspend user accounts
    /// </summary>
    /// <param name="request">Bulk unsuspend request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unsuspended user data</returns>
    [HttpPost("v{version:apiVersion}/users:unsuspend")]
    [EndpointSummary("Bulk unsuspend user accounts")]
    [EndpointDescription("Unsuspends multiple user accounts at once.")]
    [ProducesResponseType<BulkUnsuspendUsersResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUnsuspendUsers([FromBody] BulkUnsuspendUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        BulkUnsuspendUsersResult result = await sender.Send(new BulkUnsuspendUsersCommand(request.UserIds), ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk undelete soft-deleted users
    /// </summary>
    /// <param name="request">Bulk undelete request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Restored user data</returns>
    [HttpPost("v{version:apiVersion}/users:undelete")]
    [EndpointSummary("Bulk undelete soft-deleted users")]
    [EndpointDescription("Restores multiple soft-deleted users at once.")]
    [ProducesResponseType<BulkRestoreUsersResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUndeleteUsers([FromBody] BulkRestoreUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        BulkRestoreUsersResult result = await sender.Send(new BulkRestoreUsersCommand(request.UserIds), ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Bulk hard delete users (irreversible purge)
    /// </summary>
    /// <param name="request">Bulk purge request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/users:purge")]
    [EndpointSummary("Bulk hard delete users (irreversible purge)")]
    [EndpointDescription("Permanently deletes multiple users. Admin operation requiring proper authorization.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> BulkPurgeUsers([FromBody] BulkPurgeUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await sender.Send(new BulkPurgeUsersCommand(request.UserIds, request.Strategy), ct).ConfigureAwait(false);

        return NoContent();
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
    [EndpointSummary("Check if user exists by ID")]
    [EndpointDescription("Checks if a user exists by ID without returning the body.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckUserExistsById(Guid userId, CancellationToken ct)
    {
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
    [EndpointSummary("Get user by ID")]
    [EndpointDescription("Retrieves detailed information for a specific user by their unique identifier.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(Guid userId, CancellationToken ct)
    {
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
    [EndpointSummary("Partially update user by ID")]
    [EndpointDescription("Updates specific fields of a user by ID.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchUserById(Guid userId, [FromBody] UpdateUserRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new UpdateUserCommand(userId, body.Name!, body.PhoneNumber);
        UserDto? user = await sender.Send(command, ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    ///     Update user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="body">Complete user data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated user</returns>
    [HttpPut("v{version:apiVersion}/users/{userId:guid}")]
    [EndpointSummary("Update user by ID")]
    [EndpointDescription("Fully updates a user by ID with complete user data.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserById(Guid userId, [FromBody] CreateUserRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new UpdateUserCommand(userId, body.Name, body.PhoneNumber);
        UserDto? user = await sender.Send(command, ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    ///     Soft delete user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("v{version:apiVersion}/users/{userId:guid}")]
    [EndpointSummary("Soft delete user by ID")]
    [EndpointDescription("Soft deletes a user by ID (can be restored).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserById(Guid userId, CancellationToken ct)
    {
        await sender.Send(new DeleteUserCommand(userId), ct).ConfigureAwait(false);

        return NoContent();
    }

    #endregion

    #region Individual User Actions - /v1/users/{userId}:action

    /// <summary>
    ///     Activate user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Activated user data</returns>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}:activate")]
    [EndpointSummary("Activate user account")]
    [EndpointDescription("Activates a user account by ID.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateUser(Guid userId, CancellationToken ct)
    {
        UserDto? user = await sender.Send(new ActivateUserCommand(userId), ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    ///     Deactivate user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Deactivated user data</returns>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}:deactivate")]
    [EndpointSummary("Deactivate user account")]
    [EndpointDescription("Deactivates a user account by ID.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken ct)
    {
        UserDto? user = await sender.Send(new DeactivateUserCommand(userId), ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    ///     Suspend user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Suspended user data</returns>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}:suspend")]
    [EndpointSummary("Suspend user account")]
    [EndpointDescription("Suspends a user account by ID.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SuspendUser(Guid userId, CancellationToken ct)
    {
        UserDto? user = await sender.Send(new SuspendUserCommand(userId), ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    ///     Unsuspend user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unsuspended user data</returns>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}:unsuspend")]
    [EndpointSummary("Unsuspend user account")]
    [EndpointDescription("Unsuspends a user account by ID.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnsuspendUser(Guid userId, CancellationToken ct)
    {
        UserDto? user = await sender.Send(new UnsuspendUserCommand(userId), ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    ///     Undelete soft-deleted user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Restored user data</returns>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}:undelete")]
    [EndpointSummary("Undelete soft-deleted user by ID")]
    [EndpointDescription("Restores a soft-deleted user by ID.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UndeleteUser(Guid userId, CancellationToken ct)
    {
        UserDto? user = await sender.Send(new RestoreUserCommand(userId), ct).ConfigureAwait(false);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    ///     Hard delete user by ID (irreversible purge)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}:purge")]
    [EndpointSummary("Hard delete user by ID (irreversible purge)")]
    [EndpointDescription("Permanently deletes a user by ID (irreversible).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> PurgeUser(Guid userId, CancellationToken ct)
    {
        var strategy = PurgeStrategy.GracePeriod;
        await sender.Send(new PurgeUserCommand(userId, strategy), ct).ConfigureAwait(false);

        return NoContent();
    }

    #endregion
}
