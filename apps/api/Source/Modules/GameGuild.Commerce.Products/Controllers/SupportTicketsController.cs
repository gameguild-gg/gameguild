using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Products;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/support/tickets")]
[Microsoft.AspNetCore.Http.Tags("support/tickets")]
[Authorize]
[RequirePermission(ProductsPermission.Keys.Manage)]
public sealed class SupportTicketsController(ISender sender) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<PagedResult<SupportTicketDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupportTicketDto>>> List(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] SupportTicketStatus? status = null,
        [FromQuery] SupportTicketPriority? priority = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetSupportTicketsQuery(tenantId, status, priority, search, skip, take),
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    [HttpGet("{ticketId:guid}", Name = "GetSupportTicketById")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupportTicketDto>> GetById(
        Guid ticketId,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
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
        var result = await sender.Send(
            new CreateSupportTicketCommand(
                request.TenantId,
                request.CustomerId,
                request.CustomerName,
                request.ReporterUserId,
                request.ReporterName,
                request.ReporterEmail,
                request.Subject,
                request.Body,
                request.Priority,
                request.Category),
            cancellationToken).ConfigureAwait(false);

        return CreatedAtRoute(
            "GetSupportTicketById",
            new { ticketId = result.Id, tenantId = result.TenantId, version = "1.0" },
            result);
    }

    [HttpPost("{ticketId:guid}/messages")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketDto>> AddMessage(
        Guid ticketId,
        [FromBody] AddSupportTicketMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new AddSupportTicketMessageCommand(
                ticketId,
                request.TenantId,
                request.AuthorUserId,
                request.AuthorName,
                request.AuthorEmail,
                request.AuthorType,
                request.Body,
                request.IsInternal),
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost("{ticketId:guid}:assign")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketDto>> Assign(
        Guid ticketId,
        [FromBody] AssignSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new AssignSupportTicketCommand(ticketId, request.TenantId, request.AgentUserId, request.AgentName),
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost("{ticketId:guid}:resolve")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketDto>> Resolve(
        Guid ticketId,
        [FromBody] ResolveSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ResolveSupportTicketCommand(
                ticketId,
                request.TenantId,
                request.AgentUserId,
                request.AgentName,
                request.ResolutionSummary),
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost("{ticketId:guid}:close")]
    [ProducesResponseType<SupportTicketDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketDto>> Close(
        Guid ticketId,
        [FromBody] CloseSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new CloseSupportTicketCommand(
                ticketId,
                request.TenantId,
                request.AgentUserId,
                request.AgentName,
                request.ClosingNotes),
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }
}

public sealed record CreateSupportTicketRequest(
    Guid TenantId,
    Guid CustomerId,
    string CustomerName,
    Guid ReporterUserId,
    string ReporterName,
    string? ReporterEmail,
    string Subject,
    string Body,
    SupportTicketPriority Priority = SupportTicketPriority.Normal,
    string? Category = null);

public sealed record AddSupportTicketMessageRequest(
    Guid TenantId,
    Guid AuthorUserId,
    string AuthorName,
    string? AuthorEmail,
    SupportTicketMessageAuthorType AuthorType,
    string Body,
    bool IsInternal = false);

public sealed record AssignSupportTicketRequest(Guid TenantId, Guid AgentUserId, string AgentName);

public sealed record ResolveSupportTicketRequest(
    Guid TenantId,
    Guid AgentUserId,
    string AgentName,
    string ResolutionSummary);

public sealed record CloseSupportTicketRequest(
    Guid TenantId,
    Guid AgentUserId,
    string AgentName,
    string? ClosingNotes = null);
