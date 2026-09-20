using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Products;

/// <summary>
///     Customer self-service support endpoints. Identity and tenant values are derived from
///     the authenticated actor so callers cannot submit tickets on behalf of another tenant.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/support/tickets/mine")]
[Microsoft.AspNetCore.Http.Tags("support/tickets/self-service")]
[Authorize]
public sealed class MySupportTicketsController(
    ISender sender,
    IActorContextAccessor actorContextAccessor,
    IAuthorizationTenantContext tenantContext) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<PagedResult<SupportTicketDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SupportTicketDto>>> List(
        [FromQuery] SupportTicketStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCustomerContext(out var context))
        {
            return Forbid();
        }

        var (userId, tenantId, _, _) = context;
        var result = await sender.Send(
            new GetSupportTicketsQuery(tenantId, status, null, null, skip, take, userId, IncludeInternalMessages: false),
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SupportTicketDto>> Create(
        [FromBody] CreateMySupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryGetCustomerContext(out var context))
        {
            return Forbid();
        }

        var (userId, tenantId, reporterName, reporterEmail) = context;
        var result = await sender.Send(
            new CreateSupportTicketCommand(
                tenantId,
                userId,
                reporterName,
                userId,
                reporterName,
                reporterEmail,
                request.Subject,
                request.Body,
                request.Priority,
                request.Category),
            cancellationToken).ConfigureAwait(false);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("{ticketId:guid}/messages")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDto>> AddMessage(
        Guid ticketId,
        [FromBody] AddMySupportTicketMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryGetCustomerContext(out var context))
        {
            return Forbid();
        }

        var (userId, tenantId, reporterName, reporterEmail) = context;
        var existing = await sender.Send(new GetSupportTicketByIdQuery(ticketId, tenantId, IncludeInternalMessages: false), cancellationToken)
            .ConfigureAwait(false);

        if (existing is null || existing.CustomerId != userId)
        {
            return NotFound();
        }

        await sender.Send(
            new AddSupportTicketMessageCommand(
                ticketId,
                tenantId,
                userId,
                reporterName,
                reporterEmail,
                SupportTicketMessageAuthorType.Customer,
                request.Body,
                false),
            cancellationToken).ConfigureAwait(false);

        var result = await sender.Send(
            new GetSupportTicketByIdQuery(ticketId, tenantId, IncludeInternalMessages: false),
            cancellationToken).ConfigureAwait(false);

        return Ok(result!);
    }

    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDto>> GetById(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCustomerContext(out var context))
        {
            return Forbid();
        }

        var (userId, tenantId, _, _) = context;
        var ticket = await sender.Send(
            new GetSupportTicketByIdQuery(ticketId, tenantId, IncludeInternalMessages: false),
            cancellationToken).ConfigureAwait(false);
        return ticket is null || ticket.CustomerId != userId ? NotFound() : Ok(ticket);
    }

    private bool TryGetCustomerContext(out (Guid UserId, Guid TenantId, string Name, string? Email) context)
    {
        context = default;
        var actor = actorContextAccessor.ActorContext;
        var userId = actor.SubjectIdAsGuid;
        var tenantId = tenantContext.TenantId;
        if (!userId.HasValue || !tenantId.HasValue || !actor.IsAuthenticated || actor.TenantId != tenantId)
        {
            return false;
        }

        var name = actor.TypedAttributes.DisplayName
            ?? actor.TypedAttributes.FullName
            ?? actor.TypedAttributes.Email
            ?? "Customer";

        context = (userId.Value, tenantId.Value, name, actor.TypedAttributes.Email);
        return true;
    }
}

public sealed record CreateMySupportTicketRequest(
    string Subject,
    string Body,
    SupportTicketPriority Priority = SupportTicketPriority.Normal,
    string? Category = null);

public sealed record AddMySupportTicketMessageRequest(string Body);
