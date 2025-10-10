using GameGuild.Database;
using GameGuild.Modules.Resources.Contexts;
using GameGuild.Modules.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Modules.Resources.Events;

/// <summary>
/// Outbox pattern implementation for reliable event publishing
/// </summary>
public class OutboxEventPublisher : IOutboxEventPublisher
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OutboxEventPublisher> _logger;

    public OutboxEventPublisher(
        ApplicationDbContext context,
        ILogger<OutboxEventPublisher> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null,
            Retries = 0
        };

        _context.Set<OutboxEvent>().Add(outboxEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Published event {EventType} to outbox with ID {EventId}",
            outboxEvent.EventType, outboxEvent.Id);
    }
}

/// <summary>
/// Outbox event entity for reliable event delivery
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int Retries { get; set; }
    public string? ErrorMessage { get; set; }
}
