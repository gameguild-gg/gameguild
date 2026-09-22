using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

public sealed record SupportTicketDto(
    Guid Id,
    Guid? TenantId,
    Guid CustomerId,
    string CustomerName,
    Guid ReporterUserId,
    string ReporterName,
    string? ReporterEmail,
    string Subject,
    string? Category,
    SupportTicketStatus Status,
    SupportTicketPriority Priority,
    Guid? AssignedToUserId,
    string? AssignedToName,
    DateTime OpenedAt,
    DateTime? FirstResponseAt,
    DateTime? ResponseDueBy,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    string? ResolutionSummary,
    DateTime? LastMessageAt,
    string? LastMessagePreview,
    int MessageCount,
    IReadOnlyList<SupportTicketMessageDto> Messages);

public sealed record SupportTicketMessageDto(
    Guid Id,
    Guid TicketId,
    Guid AuthorUserId,
    string AuthorName,
    string? AuthorEmail,
    SupportTicketMessageAuthorType AuthorType,
    string Body,
    bool IsInternal,
    DateTime CreatedAt);

public sealed record CreateSupportTicketCommand(
    Guid TenantId,
    Guid CustomerId,
    string CustomerName,
    Guid ReporterUserId,
    string ReporterName,
    string? ReporterEmail,
    string Subject,
    string Body,
    SupportTicketPriority Priority = SupportTicketPriority.Normal,
    string? Category = null) : ICommand<SupportTicketDto>;

public sealed record AddSupportTicketMessageCommand(
    Guid TicketId,
    Guid TenantId,
    Guid AuthorUserId,
    string AuthorName,
    string? AuthorEmail,
    SupportTicketMessageAuthorType AuthorType,
    string Body,
    bool IsInternal = false) : ICommand<SupportTicketDto>;

public sealed record AssignSupportTicketCommand(
    Guid TicketId,
    Guid TenantId,
    Guid AgentUserId,
    string AgentName) : ICommand<SupportTicketDto>;

public sealed record ResolveSupportTicketCommand(
    Guid TicketId,
    Guid TenantId,
    Guid AgentUserId,
    string AgentName,
    string ResolutionSummary) : ICommand<SupportTicketDto>;

public sealed record CloseSupportTicketCommand(
    Guid TicketId,
    Guid TenantId,
    Guid AgentUserId,
    string AgentName,
    string? ClosingNotes = null) : ICommand<SupportTicketDto>;

public sealed record StartSupportTicketCommand(Guid TicketId, Guid TenantId) : ICommand<SupportTicketDto>;

public sealed record ReopenSupportTicketCommand(Guid TicketId, Guid TenantId) : ICommand<SupportTicketDto>;

public sealed record ChangeSupportTicketPriorityCommand(
    Guid TicketId,
    Guid TenantId,
    SupportTicketPriority Priority) : ICommand<SupportTicketDto>;

public sealed record SupportTicketSummaryDto(
    int Open,
    int InProgress,
    int ResolvedToday,
    int HighOrUrgent);

public sealed record GetSupportTicketSummaryQuery(Guid TenantId) : IQuery<SupportTicketSummaryDto>;

public sealed record GetSupportTicketsQuery(
    Guid? TenantId = null,
    SupportTicketStatus? Status = null,
    SupportTicketPriority? Priority = null,
    string? Search = null,
    int Skip = 0,
    int Take = 50,
    Guid? CustomerId = null,
    string? Category = null,
    Guid? AssignedToUserId = null,
    bool IncludeInternalMessages = true) : IQuery<PagedResult<SupportTicketDto>>;

public sealed record GetSupportTicketByIdQuery(
    Guid TicketId,
    Guid? TenantId = null,
    bool IncludeInternalMessages = true) : IQuery<SupportTicketDto?>;

public sealed class CreateSupportTicketCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateSupportTicketCommand, SupportTicketDto>
{
    public async Task<SupportTicketDto> Handle(CreateSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = SupportTicket.Open(
            request.TenantId,
            request.CustomerId,
            request.CustomerName,
            request.ReporterUserId,
            request.ReporterName,
            request.ReporterEmail,
            request.Subject,
            request.Body,
            request.Priority,
            request.Category);

        await context.Set<SupportTicket>().AddAsync(ticket, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ticket.ToDto();
    }
}

public sealed class AddSupportTicketMessageCommandHandler(IApplicationDbContext context)
    : ICommandHandler<AddSupportTicketMessageCommand, SupportTicketDto>
{
    public async Task<SupportTicketDto> Handle(AddSupportTicketMessageCommand request, CancellationToken cancellationToken)
    {
        var ticket = await SupportTicketHandlers.LoadTicketAsync(context, request.TicketId, request.TenantId, cancellationToken).ConfigureAwait(false);

        var message = ticket.AddMessage(
            request.AuthorUserId,
            request.AuthorName,
            request.AuthorEmail,
            request.AuthorType,
            request.Body,
            request.IsInternal);

        await context.Set<SupportTicketMessage>().AddAsync(message, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.ToDto();
    }
}

public sealed class AssignSupportTicketCommandHandler(IApplicationDbContext context)
    : ICommandHandler<AssignSupportTicketCommand, SupportTicketDto>
{
    public async Task<SupportTicketDto> Handle(AssignSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await SupportTicketHandlers.LoadTicketAsync(context, request.TicketId, request.TenantId, cancellationToken).ConfigureAwait(false);
        ticket.Assign(request.AgentUserId, request.AgentName);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.ToDto();
    }
}

public sealed class ResolveSupportTicketCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ResolveSupportTicketCommand, SupportTicketDto>
{
    public async Task<SupportTicketDto> Handle(ResolveSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await SupportTicketHandlers.LoadTicketAsync(context, request.TicketId, request.TenantId, cancellationToken).ConfigureAwait(false);
        var message = ticket.Resolve(request.AgentUserId, request.AgentName, request.ResolutionSummary);
        await context.Set<SupportTicketMessage>().AddAsync(message, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.ToDto();
    }
}

public sealed class CloseSupportTicketCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CloseSupportTicketCommand, SupportTicketDto>
{
    public async Task<SupportTicketDto> Handle(CloseSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await SupportTicketHandlers.LoadTicketAsync(context, request.TicketId, request.TenantId, cancellationToken).ConfigureAwait(false);
        var message = ticket.Close(request.AgentUserId, request.AgentName, request.ClosingNotes);
        if (message is not null)
        {
            await context.Set<SupportTicketMessage>().AddAsync(message, cancellationToken).ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.ToDto();
    }
}

public sealed class StartSupportTicketCommandHandler(IApplicationDbContext context)
    : ICommandHandler<StartSupportTicketCommand, SupportTicketDto>
{
    public async Task<SupportTicketDto> Handle(StartSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await SupportTicketHandlers.LoadTicketAsync(
            context, request.TicketId, request.TenantId, cancellationToken).ConfigureAwait(false);
        ticket.Start();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.ToDto();
    }
}

public sealed class ReopenSupportTicketCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ReopenSupportTicketCommand, SupportTicketDto>
{
    public async Task<SupportTicketDto> Handle(ReopenSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await SupportTicketHandlers.LoadTicketAsync(
            context, request.TicketId, request.TenantId, cancellationToken).ConfigureAwait(false);
        ticket.Reopen();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.ToDto();
    }
}

public sealed class ChangeSupportTicketPriorityCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ChangeSupportTicketPriorityCommand, SupportTicketDto>
{
    public async Task<SupportTicketDto> Handle(
        ChangeSupportTicketPriorityCommand request,
        CancellationToken cancellationToken)
    {
        var ticket = await SupportTicketHandlers.LoadTicketAsync(
            context, request.TicketId, request.TenantId, cancellationToken).ConfigureAwait(false);
        ticket.ChangePriority(request.Priority);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.ToDto();
    }
}

public sealed class GetSupportTicketSummaryQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSupportTicketSummaryQuery, SupportTicketSummaryDto>
{
    public async Task<SupportTicketSummaryDto> Handle(
        GetSupportTicketSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var startOfToday = DateTime.UtcNow.Date;
        var tickets = context.Set<SupportTicket>().AsNoTracking()
            .Where(ticket => ticket.TenantId == request.TenantId && ticket.DeletedAt == null);
        return new SupportTicketSummaryDto(
            await tickets.CountAsync(ticket => ticket.Status == SupportTicketStatus.Open, cancellationToken).ConfigureAwait(false),
            await tickets.CountAsync(ticket => ticket.Status == SupportTicketStatus.InProgress, cancellationToken).ConfigureAwait(false),
            await tickets.CountAsync(ticket => ticket.ResolvedAt >= startOfToday, cancellationToken).ConfigureAwait(false),
            await tickets.CountAsync(ticket =>
                (ticket.Status == SupportTicketStatus.Open || ticket.Status == SupportTicketStatus.InProgress) &&
                (ticket.Priority == SupportTicketPriority.High || ticket.Priority == SupportTicketPriority.Urgent),
                cancellationToken).ConfigureAwait(false));
    }
}

public sealed class GetSupportTicketsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSupportTicketsQuery, PagedResult<SupportTicketDto>>
{
    public async Task<PagedResult<SupportTicketDto>> Handle(GetSupportTicketsQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 100);
        var skip = Math.Max(0, request.Skip);
        var query = context.Set<SupportTicket>()
            .AsNoTracking()
            .Include(ticket => ticket.Messages)
            .AsQueryable();

        if (request.TenantId.HasValue)
        {
            query = query.Where(ticket => ticket.TenantId == request.TenantId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(ticket => ticket.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(ticket => ticket.Priority == request.Priority.Value);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(ticket => ticket.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim().ToLowerInvariant();
            query = query.Where(ticket => ticket.Category != null && ticket.Category.ToLower() == category);
        }

        if (request.AssignedToUserId.HasValue)
        {
            query = query.Where(ticket => ticket.AssignedToUserId == request.AssignedToUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(ticket =>
                ticket.Subject.ToLower().Contains(search) ||
                ticket.CustomerName.ToLower().Contains(search) ||
                (ticket.Category != null && ticket.Category.ToLower().Contains(search)) ||
                (ticket.LastMessagePreview != null && ticket.LastMessagePreview.ToLower().Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var tickets = await query
            .OrderByDescending(ticket => ticket.LastMessageAt ?? ticket.OpenedAt)
            .ThenByDescending(ticket => ticket.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<SupportTicketDto>(
            tickets.Select(ticket => ticket.ToDto(request.IncludeInternalMessages)), total, skip, take);
    }
}

public sealed class GetSupportTicketByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSupportTicketByIdQuery, SupportTicketDto?>
{
    public async Task<SupportTicketDto?> Handle(GetSupportTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<SupportTicket>()
            .AsNoTracking()
            .Include(ticket => ticket.Messages)
            .Where(ticket => ticket.Id == request.TicketId);

        if (request.TenantId.HasValue)
        {
            query = query.Where(ticket => ticket.TenantId == request.TenantId.Value);
        }

        var ticket = await query.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return ticket?.ToDto(request.IncludeInternalMessages);
    }
}

public static class SupportTicketMappingExtensions
{
    public static SupportTicketDto ToDto(this SupportTicket ticket, bool includeInternalMessages = true)
    {
        var messages = ticket.Messages
            .Where(message => includeInternalMessages || !message.IsInternal)
            .OrderBy(message => message.CreatedAt)
            .Select(message => message.ToDto())
            .ToList();

        return new SupportTicketDto(
            ticket.Id,
            ticket.TenantId,
            ticket.CustomerId,
            ticket.CustomerName,
            ticket.ReporterUserId,
            ticket.ReporterName,
            ticket.ReporterEmail,
            ticket.Subject,
            ticket.Category,
            ticket.Status,
            ticket.Priority,
            ticket.AssignedToUserId,
            ticket.AssignedToName,
            ticket.OpenedAt,
            ticket.FirstResponseAt,
            ticket.ResponseDueBy,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            ticket.ResolutionSummary,
            ticket.LastMessageAt,
            ticket.LastMessagePreview,
            messages.Count,
            messages);
    }

    public static SupportTicketMessageDto ToDto(this SupportTicketMessage message)
        => new(
            message.Id,
            message.TicketId,
            message.AuthorUserId,
            message.AuthorName,
            message.AuthorEmail,
            message.AuthorType,
            message.Body,
            message.IsInternal,
            message.CreatedAt);
}

file static class SupportTicketHandlers
{
    public static async Task<SupportTicket> LoadTicketAsync(
        IApplicationDbContext context,
        Guid ticketId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var ticket = await context.Set<SupportTicket>()
            .Include(current => current.Messages)
            .SingleOrDefaultAsync(
                current => current.Id == ticketId && current.TenantId == tenantId,
                cancellationToken)
            .ConfigureAwait(false);

        return ticket ?? throw new KeyNotFoundException($"Support ticket {ticketId} was not found.");
    }
}
