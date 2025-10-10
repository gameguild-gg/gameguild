namespace GameGuild.Modules.Support.Ticketing;

/// <summary>
/// Represents a support ticket.
/// </summary>
public sealed class SupportTicket
{
    public Guid Id { get; set; }
    public required string Subject { get; set; }
    public required string Description { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public required string Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime SlaDeadline { get; set; }
}

/// <summary>
/// Status of a support ticket.
/// </summary>
public enum TicketStatus
{
    New,
    Open,
    Pending,
    Resolved,
    Closed
}

/// <summary>
/// Priority level of a support ticket.
/// </summary>
public enum TicketPriority
{
    Low,
    Medium,
    High,
    Urgent
}

/// <summary>
/// Represents a ticket message/reply.
/// </summary>
public sealed class TicketMessage
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public required string Content { get; set; }
    public bool IsInternal { get; set; }
    public List<string> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents an SLA configuration.
/// </summary>
public sealed class SlaConfiguration
{
    public TicketPriority Priority { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public TimeSpan ResolutionTime { get; set; }
}

/// <summary>
/// Result of ticket creation operation.
/// </summary>
public sealed class TicketCreationResult
{
    public SupportTicket Ticket { get; set; } = null!;
    public string ExternalTicketId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
}

/// <summary>
/// Satisfaction survey for resolved tickets.
/// </summary>
public sealed class SatisfactionSurvey
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Service interface for support ticketing operations.
/// </summary>
public interface ISupportTicketingService
{
    /// <summary>
    /// Creates a new support ticket.
    /// </summary>
    Task<TicketCreationResult> CreateTicketAsync(
        string subject,
        string description,
        Guid customerId,
        string category,
        TicketPriority priority = TicketPriority.Medium,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing ticket.
    /// </summary>
    Task<SupportTicket> UpdateTicketAsync(
        Guid ticketId,
        string? subject = null,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        Guid? assignedAgentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a ticket.
    /// </summary>
    Task CloseTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a ticket to an agent.
    /// </summary>
    Task AssignTicketAsync(
        Guid ticketId,
        Guid agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message/reply to a ticket.
    /// </summary>
    Task<TicketMessage> AddMessageAsync(
        Guid ticketId,
        Guid authorId,
        string content,
        bool isInternal = false,
        List<string>? attachments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ticket messages.
    /// </summary>
    Task<IReadOnlyList<TicketMessage>> GetMessagesAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks SLA compliance for a ticket.
    /// </summary>
    Task<bool> CheckSlaComplianceAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Escalates a ticket based on SLA breach.
    /// </summary>
    Task EscalateTicketAsync(
        Guid ticketId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds tags to a ticket for categorization.
    /// </summary>
    Task AddTagsAsync(
        Guid ticketId,
        List<string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a satisfaction survey for a resolved ticket.
    /// </summary>
    Task<SatisfactionSurvey> SubmitSurveyAsync(
        Guid ticketId,
        int rating,
        string? comment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tickets by customer.
    /// </summary>
    Task<IReadOnlyList<SupportTicket>> GetTicketsByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tickets assigned to an agent.
    /// </summary>
    Task<IReadOnlyList<SupportTicket>> GetTicketsByAgentAsync(
        Guid agentId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of support ticketing service with third-party integrations.
/// </summary>
public sealed class SupportTicketingService : ISupportTicketingService
{
    private readonly ILogger<SupportTicketingService> _logger;
    private readonly Dictionary<Guid, SupportTicket> _tickets = new();
    private readonly Dictionary<Guid, List<TicketMessage>> _messages = new();
    private readonly Dictionary<TicketPriority, SlaConfiguration> _slaConfigurations;

    public SupportTicketingService(ILogger<SupportTicketingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _slaConfigurations = InitializeSlaConfigurations();
    }

    public Task<TicketCreationResult> CreateTicketAsync(
        string subject,
        string description,
        Guid customerId,
        string category,
        TicketPriority priority = TicketPriority.Medium,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating ticket: {Subject} for customer {CustomerId}", subject, customerId);

        var slaConfig = _slaConfigurations[priority];
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Description = description,
            CustomerId = customerId,
            Status = TicketStatus.New,
            Priority = priority,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            SlaDeadline = DateTime.UtcNow.Add(slaConfig.ResponseTime)
        };

        _tickets[ticket.Id] = ticket;
        _messages[ticket.Id] = new List<TicketMessage>();

        var result = new TicketCreationResult
        {
            Ticket = ticket,
            ExternalTicketId = $"TICKET-{ticket.Id.ToString()[..8].ToUpperInvariant()}",
            ProviderName = "Internal"
        };

        return Task.FromResult(result);
    }

    public Task<SupportTicket> UpdateTicketAsync(
        Guid ticketId,
        string? subject = null,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        Guid? assignedAgentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket))
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found");
        }

        if (subject != null) ticket.Subject = subject;
        if (status.HasValue) ticket.Status = status.Value;
        if (priority.HasValue) ticket.Priority = priority.Value;
        if (assignedAgentId.HasValue) ticket.AssignedAgentId = assignedAgentId.Value;

        if (status == TicketStatus.Resolved)
        {
            ticket.ResolvedAt = DateTime.UtcNow;
        }

        _logger.LogInformation("Updated ticket: {TicketId}", ticketId);
        return Task.FromResult(ticket);
    }

    public Task CloseTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            ticket.Status = TicketStatus.Closed;
            ticket.ClosedAt = DateTime.UtcNow;
            _logger.LogInformation("Closed ticket: {TicketId}", ticketId);
        }

        return Task.CompletedTask;
    }

    public Task AssignTicketAsync(
        Guid ticketId,
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            ticket.AssignedAgentId = agentId;
            ticket.Status = TicketStatus.Open;
            _logger.LogInformation("Assigned ticket {TicketId} to agent {AgentId}", ticketId, agentId);
        }

        return Task.CompletedTask;
    }

    public Task<TicketMessage> AddMessageAsync(
        Guid ticketId,
        Guid authorId,
        string content,
        bool isInternal = false,
        List<string>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = authorId,
            Content = content,
            IsInternal = isInternal,
            Attachments = attachments ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        if (!_messages.ContainsKey(ticketId))
        {
            _messages[ticketId] = new List<TicketMessage>();
        }

        _messages[ticketId].Add(message);
        _logger.LogInformation("Added message to ticket {TicketId}", ticketId);

        return Task.FromResult(message);
    }

    public Task<IReadOnlyList<TicketMessage>> GetMessagesAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(ticketId, out var messages))
        {
            return Task.FromResult<IReadOnlyList<TicketMessage>>(messages);
        }

        return Task.FromResult<IReadOnlyList<TicketMessage>>(Array.Empty<TicketMessage>());
    }

    public Task<bool> CheckSlaComplianceAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket))
        {
            return Task.FromResult(false);
        }

        var isCompliant = DateTime.UtcNow <= ticket.SlaDeadline;
        _logger.LogInformation("Ticket {TicketId} SLA compliance: {IsCompliant}", ticketId, isCompliant);

        return Task.FromResult(isCompliant);
    }

    public Task EscalateTicketAsync(
        Guid ticketId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            ticket.Priority = ticket.Priority switch
            {
                TicketPriority.Low => TicketPriority.Medium,
                TicketPriority.Medium => TicketPriority.High,
                TicketPriority.High => TicketPriority.Urgent,
                _ => ticket.Priority
            };

            _logger.LogWarning("Escalated ticket {TicketId}: {Reason}", ticketId, reason);
        }

        return Task.CompletedTask;
    }

    public Task AddTagsAsync(
        Guid ticketId,
        List<string> tags,
        CancellationToken cancellationToken = default)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            ticket.Tags.AddRange(tags);
            _logger.LogInformation("Added {Count} tags to ticket {TicketId}", tags.Count, ticketId);
        }

        return Task.CompletedTask;
    }

    public Task<SatisfactionSurvey> SubmitSurveyAsync(
        Guid ticketId,
        int rating,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var survey = new SatisfactionSurvey
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Rating = rating,
            Comment = comment,
            SubmittedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Satisfaction survey submitted for ticket {TicketId}: {Rating}/5", ticketId, rating);
        return Task.FromResult(survey);
    }

    public Task<IReadOnlyList<SupportTicket>> GetTicketsByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var tickets = _tickets.Values
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<SupportTicket>>(tickets);
    }

    public Task<IReadOnlyList<SupportTicket>> GetTicketsByAgentAsync(
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        var tickets = _tickets.Values
            .Where(t => t.AssignedAgentId == agentId)
            .OrderBy(t => t.SlaDeadline)
            .ToList();

        return Task.FromResult<IReadOnlyList<SupportTicket>>(tickets);
    }

    private Dictionary<TicketPriority, SlaConfiguration> InitializeSlaConfigurations()
    {
        return new Dictionary<TicketPriority, SlaConfiguration>
        {
            [TicketPriority.Low] = new SlaConfiguration
            {
                Priority = TicketPriority.Low,
                ResponseTime = TimeSpan.FromHours(24),
                ResolutionTime = TimeSpan.FromDays(7)
            },
            [TicketPriority.Medium] = new SlaConfiguration
            {
                Priority = TicketPriority.Medium,
                ResponseTime = TimeSpan.FromHours(8),
                ResolutionTime = TimeSpan.FromDays(3)
            },
            [TicketPriority.High] = new SlaConfiguration
            {
                Priority = TicketPriority.High,
                ResponseTime = TimeSpan.FromHours(4),
                ResolutionTime = TimeSpan.FromDays(1)
            },
            [TicketPriority.Urgent] = new SlaConfiguration
            {
                Priority = TicketPriority.Urgent,
                ResponseTime = TimeSpan.FromHours(1),
                ResolutionTime = TimeSpan.FromHours(8)
            }
        };
    }
}
