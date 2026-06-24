using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

public enum SupportTicketStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3,
    Cancelled = 4
}

public enum SupportTicketPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum SupportTicketMessageAuthorType
{
    Customer = 0,
    Agent = 1,
    System = 2
}

[Table("SupportTickets")]
[Index(nameof(TenantId), nameof(Status))]
[Index(nameof(TenantId), nameof(CustomerId))]
[Index(nameof(TenantId), nameof(Priority))]
public sealed class SupportTicket : EntityBase
{
    [Required]
    public Guid CustomerId { get; private set; }

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; private set; } = string.Empty;

    [Required]
    public Guid ReporterUserId { get; private set; }

    [Required]
    [MaxLength(150)]
    public string ReporterName { get; private set; } = string.Empty;

    [MaxLength(320)]
    public string? ReporterEmail { get; private set; }

    [Required]
    [MaxLength(180)]
    public string Subject { get; private set; } = string.Empty;

    [MaxLength(80)]
    public string? Category { get; private set; }

    public SupportTicketStatus Status { get; private set; } = SupportTicketStatus.Open;

    public SupportTicketPriority Priority { get; private set; } = SupportTicketPriority.Normal;

    public Guid? AssignedToUserId { get; private set; }

    [MaxLength(150)]
    public string? AssignedToName { get; private set; }

    public DateTime OpenedAt { get; private set; } = SystemClock.UtcNow;

    public DateTime? FirstResponseAt { get; private set; }

    public DateTime? ResponseDueBy { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    [MaxLength(1000)]
    public string? ResolutionSummary { get; private set; }

    public DateTime? LastMessageAt { get; private set; }

    [MaxLength(240)]
    public string? LastMessagePreview { get; private set; }

    public ICollection<SupportTicketMessage> Messages { get; private set; } = new List<SupportTicketMessage>();

    public static SupportTicket Open(
        Guid tenantId,
        Guid customerId,
        string customerName,
        Guid reporterUserId,
        string reporterName,
        string? reporterEmail,
        string subject,
        string body,
        SupportTicketPriority priority,
        string? category)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (reporterUserId == Guid.Empty) throw new ArgumentException("Reporter id is required.", nameof(reporterUserId));
        if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentException("Customer name is required.", nameof(customerName));
        if (string.IsNullOrWhiteSpace(reporterName)) throw new ArgumentException("Reporter name is required.", nameof(reporterName));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Message body is required.", nameof(body));

        var now = SystemClock.UtcNow;
        var ticket = new SupportTicket
        {
            TenantId = tenantId,
            CustomerId = customerId,
            CustomerName = customerName.Trim(),
            ReporterUserId = reporterUserId,
            ReporterName = reporterName.Trim(),
            ReporterEmail = NormalizeNullable(reporterEmail),
            Subject = subject.Trim(),
            Category = NormalizeNullable(category),
            Priority = priority,
            Status = SupportTicketStatus.Open,
            OpenedAt = now,
            ResponseDueBy = now.Add(GetResponseWindow(priority))
        };

        ticket.AddMessage(reporterUserId, reporterName, reporterEmail, SupportTicketMessageAuthorType.Customer, body, false);
        return ticket;
    }

    public SupportTicketMessage AddMessage(
        Guid authorUserId,
        string authorName,
        string? authorEmail,
        SupportTicketMessageAuthorType authorType,
        string body,
        bool isInternal)
    {
        if (Status is SupportTicketStatus.Closed or SupportTicketStatus.Cancelled)
            throw new InvalidOperationException("Closed or cancelled support tickets cannot receive new messages.");

        if (authorUserId == Guid.Empty) throw new ArgumentException("Author id is required.", nameof(authorUserId));
        if (string.IsNullOrWhiteSpace(authorName)) throw new ArgumentException("Author name is required.", nameof(authorName));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Message body is required.", nameof(body));

        var message = SupportTicketMessage.Create(Id, TenantId!.Value, authorUserId, authorName, authorEmail, authorType, body, isInternal);
        Messages.Add(message);

        if (authorType == SupportTicketMessageAuthorType.Agent)
        {
            if (Status == SupportTicketStatus.Open)
            {
                Status = SupportTicketStatus.InProgress;
            }

            FirstResponseAt ??= message.CreatedAt;
        }

        LastMessageAt = message.CreatedAt;
        LastMessagePreview = BuildPreview(body);
        Touch();

        return message;
    }

    public void Assign(Guid agentUserId, string agentName)
    {
        if (Status is SupportTicketStatus.Closed or SupportTicketStatus.Cancelled)
            throw new InvalidOperationException("Closed or cancelled support tickets cannot be assigned.");

        if (agentUserId == Guid.Empty) throw new ArgumentException("Agent id is required.", nameof(agentUserId));
        if (string.IsNullOrWhiteSpace(agentName)) throw new ArgumentException("Agent name is required.", nameof(agentName));

        AssignedToUserId = agentUserId;
        AssignedToName = agentName.Trim();
        if (Status == SupportTicketStatus.Open)
        {
            Status = SupportTicketStatus.InProgress;
        }

        Touch();
    }

    public SupportTicketMessage Resolve(Guid agentUserId, string agentName, string summary)
    {
        if (Status is SupportTicketStatus.Closed or SupportTicketStatus.Cancelled)
            throw new InvalidOperationException("Closed or cancelled support tickets cannot be resolved.");

        if (string.IsNullOrWhiteSpace(summary)) throw new ArgumentException("Resolution summary is required.", nameof(summary));

        Assign(agentUserId, agentName);
        Status = SupportTicketStatus.Resolved;
        ResolvedAt = SystemClock.UtcNow;
        ResolutionSummary = summary.Trim();
        var message = AddMessage(agentUserId, agentName, null, SupportTicketMessageAuthorType.Agent, ResolutionSummary, true);
        Status = SupportTicketStatus.Resolved;
        Touch();
        return message;
    }

    public SupportTicketMessage? Close(Guid agentUserId, string agentName, string? closingNotes)
    {
        if (Status == SupportTicketStatus.Cancelled)
            throw new InvalidOperationException("Cancelled support tickets cannot be closed.");

        if (Status == SupportTicketStatus.Closed)
            return null;

        Assign(agentUserId, agentName);
        SupportTicketMessage? message = null;

        if (!string.IsNullOrWhiteSpace(closingNotes))
        {
            message = AddMessage(agentUserId, agentName, null, SupportTicketMessageAuthorType.Agent, closingNotes, true);
        }

        Status = SupportTicketStatus.Closed;
        ClosedAt = SystemClock.UtcNow;
        Touch();
        return message;
    }

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TimeSpan GetResponseWindow(SupportTicketPriority priority)
        => priority switch
        {
            SupportTicketPriority.Urgent => TimeSpan.FromHours(2),
            SupportTicketPriority.High => TimeSpan.FromHours(8),
            SupportTicketPriority.Normal => TimeSpan.FromHours(24),
            _ => TimeSpan.FromHours(48)
        };

    private static string BuildPreview(string body)
    {
        var trimmed = body.Trim();
        return trimmed.Length <= 240 ? trimmed : trimmed[..237] + "...";
    }
}

[Table("SupportTicketMessages")]
[Index(nameof(TenantId), nameof(TicketId))]
public sealed class SupportTicketMessage : EntityBase
{
    [Required]
    public Guid TicketId { get; private set; }

    public SupportTicket? Ticket { get; private set; }

    [Required]
    public Guid AuthorUserId { get; private set; }

    [Required]
    [MaxLength(150)]
    public string AuthorName { get; private set; } = string.Empty;

    [MaxLength(320)]
    public string? AuthorEmail { get; private set; }

    public SupportTicketMessageAuthorType AuthorType { get; private set; }

    [Required]
    [MaxLength(4000)]
    public string Body { get; private set; } = string.Empty;

    public bool IsInternal { get; private set; }

    public static SupportTicketMessage Create(
        Guid ticketId,
        Guid tenantId,
        Guid authorUserId,
        string authorName,
        string? authorEmail,
        SupportTicketMessageAuthorType authorType,
        string body,
        bool isInternal)
    {
        if (ticketId == Guid.Empty) throw new ArgumentException("Ticket id is required.", nameof(ticketId));
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (authorUserId == Guid.Empty) throw new ArgumentException("Author id is required.", nameof(authorUserId));
        if (string.IsNullOrWhiteSpace(authorName)) throw new ArgumentException("Author name is required.", nameof(authorName));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Message body is required.", nameof(body));

        return new SupportTicketMessage
        {
            TicketId = ticketId,
            TenantId = tenantId,
            AuthorUserId = authorUserId,
            AuthorName = authorName.Trim(),
            AuthorEmail = string.IsNullOrWhiteSpace(authorEmail) ? null : authorEmail.Trim(),
            AuthorType = authorType,
            Body = body.Trim(),
            IsInternal = isInternal
        };
    }
}
