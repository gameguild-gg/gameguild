using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Controller for managing user's tenant memberships.
///     Provides user-centric view of tenant memberships (similar to Discord's "My Servers").
/// </summary>
/// <remarks>
///     This controller is in the Tenants module but serves user-centric endpoints
///     to maintain proper module boundaries while providing intuitive API paths.
/// </remarks>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("users/memberships")]
[Authorize]
public sealed class UserMembershipsController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Add a user to a tenant membership.
    ///     Useful for assigning a user to a workspace they can actively switch into.
    /// </summary>
    /// <param name="userId">The user receiving the tenant membership</param>
    /// <param name="body">Membership details including tenant and role</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Membership creation result</returns>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/memberships")]
    [Authorize(Policy = Policies.UsersEditSelf)]
    [EndpointSummary("Add a tenant membership for a user")]
    [EndpointDescription("Adds the specified user to a tenant with the requested role so the user can access that workspace.")]
    [ProducesResponseType<AddTenantMemberResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<AddTenantMemberResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<AddTenantMemberResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddUserMembership(Guid userId, [FromBody] AddUserMembershipRequest body, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        var result = await sender.Send(
                new AddTenantMemberCommand(body.TenantId, userId, body.Role, body.InvitedByEmail),
                ct
            )
            .ConfigureAwait(false);

        if (result.Success)
        {
            return StatusCode(StatusCodes.Status201Created, result);
        }

        if (result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            return NotFound(result);
        }

        return Conflict(result);
    }

    /// <summary>
    ///     Update a user's tenant role.
    ///     This is an operator/admin action used to promote or demote console access.
    /// </summary>
    /// <param name="userId">The member user ID.</param>
    /// <param name="tenantId">The tenant/workspace where the role applies.</param>
    /// <param name="body">The new role payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Role update result.</returns>
    [HttpPatch("v{version:apiVersion}/users/{userId:guid}/memberships/{tenantId:guid}/role")]
    [Authorize(Policy = Policies.TenantAdmin)]
    [EndpointSummary("Update tenant membership role")]
    [EndpointDescription("Updates the user's role in the specified tenant/workspace. Use this for console promotion/demotion flows.")]
    [ProducesResponseType<UpdateTenantMemberRoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<UpdateTenantMemberRoleResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserMembershipRole(
        Guid userId,
        Guid tenantId,
        [FromBody] UpdateUserMembershipRoleRequest body,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        var result = await sender.Send(
                new UpdateTenantMemberRoleCommand(tenantId, userId, body.Role),
                ct)
            .ConfigureAwait(false);

        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    ///     Get all tenant memberships for a user.
    ///     Returns a list of all tenants the user belongs to with their role and status.
    ///     Similar to Discord's server list showing which servers you're a member of.
    /// </summary>
    /// <param name="userId">The user ID to get memberships for</param>
    /// <param name="includeInactive">Include inactive/left memberships (default: false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of tenant memberships for the user</returns>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/memberships")]
    [Authorize(Policy = Policies.UsersReadSelf)]
    [EndpointSummary("Get all tenant memberships for a user")]
    [EndpointDescription("Returns all tenants the user belongs to, with role and membership status. Similar to Discord's 'My Servers' view.")]
    [ProducesResponseType<GetUserMembershipsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserMemberships(
        Guid userId,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = new GetUserMembershipsQuery(userId, includeInactive);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Check if user has any memberships
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>200 OK if user has memberships, 404 if none</returns>
    [HttpHead("v{version:apiVersion}/users/{userId:guid}/memberships")]
    [Authorize(Policy = Policies.UsersReadSelf)]
    [EndpointSummary("Check if user has any tenant memberships")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckUserHasMemberships(Guid userId, CancellationToken ct = default)
    {
        var query = new GetUserMembershipsQuery(userId, IncludeInactive: false);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return result.TotalCount > 0 ? Ok() : NotFound();
    }

    /// <summary>
    ///     Get count of user's active memberships
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Count of active memberships</returns>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/memberships:count")]
    [Authorize(Policy = Policies.UsersReadSelf)]
    [EndpointSummary("Get count of user's active tenant memberships")]
    [ProducesResponseType<MembershipCountResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembershipCount(Guid userId, CancellationToken ct = default)
    {
        var query = new GetUserMembershipsQuery(userId, IncludeInactive: false);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(new MembershipCountResponse { Count = result.TotalCount });
    }
}

/// <summary>
///     Response containing membership count
/// </summary>
public sealed record MembershipCountResponse
{
    /// <summary>
    ///     Number of active memberships
    /// </summary>
    public int Count { get; init; }
}

/// <summary>
///     Request body for adding a user membership.
/// </summary>
public sealed record AddUserMembershipRequest
{
    /// <summary>
    ///     Tenant to join.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    ///     Role assigned in the tenant.
    /// </summary>
    public string Role { get; init; } = "Member";

    /// <summary>
    ///     Optional inviter identifier for audit trail purposes.
    /// </summary>
    public string? InvitedByEmail { get; init; }
}

/// <summary>
///     Request body for changing a user's tenant role.
/// </summary>
public sealed record UpdateUserMembershipRoleRequest
{
    /// <summary>
    ///     New role assigned in the tenant.
    /// </summary>
    public string Role { get; init; } = "Member";
}
