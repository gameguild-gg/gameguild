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
    public async Task<ActionResult<PagedResult<SupportTicketDto>>> List(
        [FromQuery] SupportTicketStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var (userId, tenantId, _, _) = RequireCustomerContext();
        var result = await sender.Send(
            new GetSupportTicketsQuery(tenantId, status, null, null, skip, take, userId),
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SupportTicketDto>> Create(
        [FromBody] CreateMySupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (userId, tenantId, reporterName, reporterEmail) = RequireCustomerContext();
        var result = await sender.Send(
            new CreateSupportTicketCommand(
                tenantId,
                tenantId,
                "Customer workspace",
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDto>> AddMessage(
        Guid ticketId,
        [FromBody] AddMySupportTicketMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (userId, tenantId, reporterName, reporterEmail) = RequireCustomerContext();
        var existing = await sender.Send(new GetSupportTicketByIdQuery(ticketId, tenantId), cancellationToken)
            .ConfigureAwait(false);

        if (existing is null || existing.ReporterUserId != userId)
        {
            return NotFound();
        }

        var result = await sender.Send(
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

        return Ok(result);
    }

    private (Guid UserId, Guid TenantId, string Name, string? Email) RequireCustomerContext()
    {
        var actor = actorContextAccessor.ActorContext;
        var userId = actor.SubjectIdAsGuid ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("Active tenant context is required.");

        var name = actor.TypedAttributes.DisplayName
            ?? actor.TypedAttributes.FullName
            ?? actor.TypedAttributes.Email
            ?? "Customer";

        return (userId, tenantId, name, actor.TypedAttributes.Email);
    }
}

public sealed record CreateMySupportTicketRequest(
    string Subject,
    string Body,
    SupportTicketPriority Priority = SupportTicketPriority.Normal,
    string? Category = null);

public sealed record AddMySupportTicketMessageRequest(string Body);
