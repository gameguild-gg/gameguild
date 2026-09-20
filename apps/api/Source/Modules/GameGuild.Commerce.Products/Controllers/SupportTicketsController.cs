using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;

namespace GameGuild.Commerce.Products;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/support/tickets")]
[Microsoft.AspNetCore.Http.Tags("support/tickets")]
[Authorize]
public sealed class SupportTicketsController(
    ISender sender,
    IActorContextAccessor actorContextAccessor,
    IAuthorizationTenantContext tenantContext,
    IApplicationDbContext db) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<PagedResult<SupportTicketDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupportTicketDto>>> List(
        [FromQuery] SupportTicketStatus? status = null,
        [FromQuery] SupportTicketPriority? priority = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? category = null,
        [FromQuery] Guid? assignedToUserId = null)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        var result = await sender.Send(
            new GetSupportTicketsQuery(
                tenantId, status, priority, search, skip, take, customerId, category, assignedToUserId),
            cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType<SupportTicketSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketSummaryDto>> Summary(CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        return Ok(await sender.Send(new GetSupportTicketSummaryQuery(tenantId), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("agents")]
    [ProducesResponseType<IReadOnlyList<SupportAgentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupportAgentDto>>> Agents(CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        var agents = await (
                from membership in db.Set<TenantMember>().AsNoTracking()
                join user in db.Set<User>().AsNoTracking() on membership.UserId equals user.Id
                where membership.TenantId == tenantId &&
                      membership.IsActive &&
                      membership.DeletedAt == null &&
                      user.IsActive &&
                      user.DeletedAt == null
                orderby user.Name
                select new SupportAgentDto(user.Id, user.Name, user.Email))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(agents);
    }

    [HttpGet("{ticketId:guid}", Name = "GetSupportTicketById")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDto>> GetById(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        var result = await sender.Send(new GetSupportTicketByIdQuery(ticketId, tenantId), cancellationToken)
            .ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SupportTicketDto>> Create(
        [FromBody] CreateSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        var customer = await GetCustomerAsync(tenantId, request.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null) return BadRequest("Customer does not belong to the active workspace.");
        var actor = actorContextAccessor.ActorContext;
        var result = await sender.Send(
            new CreateSupportTicketCommand(
                tenantId,
                customer.Id,
                customer.Name,
                actor.SubjectIdAsGuid!.Value,
                ActorName(actor),
                actor.TypedAttributes.Email,
                request.Subject,
                request.Body,
                request.Priority,
                request.Category),
            cancellationToken).ConfigureAwait(false);
        return CreatedAtRoute("GetSupportTicketById", new { ticketId = result.Id, version = "1.0" }, result);
    }

    [HttpPost("{ticketId:guid}/messages")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketDto>> AddMessage(
        Guid ticketId,
        [FromBody] AddSupportTicketMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        var actor = actorContextAccessor.ActorContext;
        return Ok(await sender.Send(
            new AddSupportTicketMessageCommand(
                ticketId,
                tenantId,
                actor.SubjectIdAsGuid!.Value,
                ActorName(actor),
                actor.TypedAttributes.Email,
                SupportTicketMessageAuthorType.Agent,
                request.Body,
                request.IsInternal),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{ticketId:guid}:assign")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketDto>> Assign(
        Guid ticketId,
        [FromBody] AssignSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        var agent = await GetAgentAsync(tenantId, request.AgentUserId, cancellationToken).ConfigureAwait(false);
        if (agent is null) return BadRequest("Assignee must be an active member of this workspace.");
        return Ok(await sender.Send(
            new AssignSupportTicketCommand(ticketId, tenantId, agent.Id, agent.Name),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{ticketId:guid}:start")]
    public async Task<ActionResult<SupportTicketDto>> Start(Guid ticketId, CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        return Ok(await sender.Send(new StartSupportTicketCommand(ticketId, tenantId), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{ticketId:guid}:priority")]
    public async Task<ActionResult<SupportTicketDto>> ChangePriority(
        Guid ticketId,
        [FromBody] ChangeSupportTicketPriorityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        return Ok(await sender.Send(
            new ChangeSupportTicketPriorityCommand(ticketId, tenantId, request.Priority),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{ticketId:guid}:resolve")]
    public async Task<ActionResult<SupportTicketDto>> Resolve(
        Guid ticketId,
        [FromBody] ResolveSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        var actor = actorContextAccessor.ActorContext;
        return Ok(await sender.Send(
            new ResolveSupportTicketCommand(
                ticketId, tenantId, actor.SubjectIdAsGuid!.Value, ActorName(actor), request.ResolutionSummary),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{ticketId:guid}:close")]
    public async Task<ActionResult<SupportTicketDto>> Close(
        Guid ticketId,
        [FromBody] CloseSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        var actor = actorContextAccessor.ActorContext;
        return Ok(await sender.Send(
            new CloseSupportTicketCommand(
                ticketId, tenantId, actor.SubjectIdAsGuid!.Value, ActorName(actor), request.ClosingNotes),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{ticketId:guid}:reopen")]
    public async Task<ActionResult<SupportTicketDto>> Reopen(Guid ticketId, CancellationToken cancellationToken = default)
    {
        if (!TryGetManagerTenant(out var tenantId)) return Forbid();
        return Ok(await sender.Send(new ReopenSupportTicketCommand(ticketId, tenantId), cancellationToken).ConfigureAwait(false));
    }

    private bool TryGetManagerTenant(out Guid tenantId)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor.IsAuthenticated &&
            (actor.HasPermission(ProductsPermission.Keys.Manage) || actor.IsInRole("PropertyManager")) &&
            actor.SubjectIdAsGuid.HasValue &&
            tenantContext.TenantId is Guid resolvedTenantId &&
            actor.TenantId == resolvedTenantId)
        {
            tenantId = resolvedTenantId;
            return true;
        }
        tenantId = Guid.Empty;
        return false;
    }

    private async Task<User?> GetCustomerAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var isCustomer = await db.Set<TenantMember>().AsNoTracking().AnyAsync(item =>
            item.TenantId == tenantId &&
            item.UserId == userId &&
            item.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        return !isCustomer
            ? null
            : await db.Set<User>().AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == userId && item.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<User?> GetAgentAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var isAgent = await db.Set<TenantMember>().AsNoTracking().AnyAsync(item =>
            item.TenantId == tenantId && item.UserId == userId && item.IsActive &&
            item.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        return !isAgent
            ? null
            : await db.Set<User>().AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == userId && item.IsActive && item.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    private static string ActorName(ActorContext actor)
        => actor.TypedAttributes.DisplayName
           ?? actor.TypedAttributes.FullName
           ?? actor.TypedAttributes.Email
           ?? "Support Agent";
}

public sealed record SupportAgentDto(Guid UserId, string Name, string Email);

public sealed record CreateSupportTicketRequest(
    Guid CustomerId,
    string Subject,
    string Body,
    SupportTicketPriority Priority = SupportTicketPriority.Normal,
    string? Category = null);

public sealed record AddSupportTicketMessageRequest(string Body, bool IsInternal = false);

public sealed record AssignSupportTicketRequest(Guid AgentUserId);

public sealed record ResolveSupportTicketRequest(string ResolutionSummary);

public sealed record CloseSupportTicketRequest(string? ClosingNotes = null);

public sealed record ChangeSupportTicketPriorityRequest(SupportTicketPriority Priority);
