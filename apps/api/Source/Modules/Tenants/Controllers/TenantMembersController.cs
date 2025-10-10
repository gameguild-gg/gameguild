using GameGuild.Authorization;
using GameGuild.Core;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// REST API controller for tenant membership management
/// Provides endpoints for managing user memberships in tenants, including hierarchy operations
/// </summary>
[ApiController]
[Route("api/tenants")]
[Authorize]
public class TenantMembersController(IMediator mediator) : ControllerBase
{
    /// <summary> Add a member to a tenant </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="request"> Add member request </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Created tenant member </returns>
    [HttpPost("{tenantId:guid}/members")]
    [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult<TenantMemberDto>> AddMember(
        Guid tenantId,
        [FromBody] AddTenantMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddTenantMemberCommand
        {
            TenantId = tenantId,
            UserId = request.UserId,
            Role = request.Role
        };

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return CreatedAtAction(nameof(GetTenantMembers), new { tenantId }, result.Value);
    }

    /// <summary> Remove a member from a tenant </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="userId"> User ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> No content if successful </returns>
    [HttpDelete("{tenantId:guid}/members/{userId:guid}")]
    [RequireTenantPermission(PermissionType.Delete)]
    public async Task<ActionResult> RemoveMember(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveTenantMemberCommand
        {
            TenantId = tenantId,
            UserId = userId
        };

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return NoContent();
    }

    /// <summary> Update a member's role in a tenant </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="userId"> User ID </param>
    /// <param name="request"> Update role request </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Updated tenant member </returns>
    [HttpPatch("{tenantId:guid}/members/{userId:guid}/role")]
    [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult<TenantMemberDto>> UpdateMemberRole(
        Guid tenantId,
        Guid userId,
        [FromBody] UpdateMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTenantMemberRoleCommand
        {
            TenantId = tenantId,
            UserId = userId,
            NewRole = request.Role
        };

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(result.Value);
    }

    /// <summary> Activate a tenant member </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="userId"> User ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Activated tenant member </returns>
    [HttpPost("{tenantId:guid}/members/{userId:guid}/activate")]
    [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult<TenantMemberDto>> ActivateMember(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new ActivateTenantMemberCommand
        {
            TenantId = tenantId,
            UserId = userId
        };

        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(result.Value);
    }

    /// <summary> Get all members of a tenant </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> List of tenant members </returns>
    [HttpGet("{tenantId:guid}/members")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<IReadOnlyList<TenantMemberDto>>> GetTenantMembers(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var query = new GetTenantMembersQuery { TenantId = tenantId };
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(result.Value);
    }

    /// <summary> Get all tenants a user is a member of </summary>
    /// <param name="userId"> User ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> List of tenant memberships </returns>
    [HttpGet("users/{userId:guid}/tenants")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<TenantMemberDto>>> GetUserTenants(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var query = new GetUserTenantsQuery { UserId = userId };
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(result.Value);
    }

    /// <summary> Assign a parent member to create hierarchy </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="userId"> User ID (member to assign parent to) </param>
    /// <param name="request"> Assign parent request </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> No content if successful </returns>
    [HttpPost("{tenantId:guid}/members/{userId:guid}/parent")]
    [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult> AssignParentMember(
        Guid tenantId,
        Guid userId,
        [FromBody] AssignParentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignParentMemberCommand(userId, request.ParentMemberId);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return NoContent();
    }

    /// <summary> Remove parent assignment from a member </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="userId"> User ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> No content if successful </returns>
    [HttpDelete("{tenantId:guid}/members/{userId:guid}/parent")]
    [RequireTenantPermission(PermissionType.Edit)]
    public async Task<ActionResult> RemoveParentMember(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveParentMemberCommand(userId);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return NoContent();
    }

    /// <summary> Get direct children of a member </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="userId"> User ID (parent member) </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> List of child members </returns>
    [HttpGet("{tenantId:guid}/members/{userId:guid}/children")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<IReadOnlyList<TenantMemberDto>>> GetMemberChildren(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var query = new GetMemberChildrenQuery(userId);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(result.Value);
    }

    /// <summary> Get complete hierarchy for a member (all descendants) </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="userId"> User ID (root member) </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Complete member hierarchy </returns>
    [HttpGet("{tenantId:guid}/members/{userId:guid}/hierarchy")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<IReadOnlyList<TenantMemberDto>>> GetMemberHierarchy(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var query = new GetMemberHierarchyQuery(userId);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(result.Value);
    }

    /// <summary> Get the entire tenant hierarchy tree </summary>
    /// <param name="tenantId"> Tenant ID </param>
    /// <param name="cancellationToken"> Cancellation token </param>
    /// <returns> Complete tenant hierarchy tree </returns>
    [HttpGet("{tenantId:guid}/hierarchy")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<IReadOnlyList<TenantMemberDto>>> GetTenantHierarchyTree(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var query = new GetTenantHierarchyTreeQuery(tenantId);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { Message = result.Error });

        return Ok(result.Value);
    }
}

/// <summary> Request to add a member to a tenant </summary>
public sealed record AddTenantMemberRequest(Guid UserId, string Role);

/// <summary> Request to update a member's role </summary>
public sealed record UpdateMemberRoleRequest(string Role);

/// <summary> Request to assign a parent member </summary>
public sealed record AssignParentRequest(Guid ParentMemberId);
