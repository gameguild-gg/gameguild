using GameGuild.Modules.Common.Outbox.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Common.Outbox;

/// <summary>
/// Database-backed implementation of outbox service
/// </summary>
internal sealed class OutboxService : IOutboxService
{
    private readonly DbContext _dbContext;

    public OutboxService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddMessageAsync(
        string messageType,
        string payload,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            MessageType = messageType,
            Payload = payload,
            CorrelationId = correlationId,
            Status = (int)OutboxMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        _dbContext.Set<OutboxMessageEntity>().Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Status == (int)OutboxMessageStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new OutboxMessage
        {
            Id = e.Id,
            MessageType = e.MessageType,
            Payload = e.Payload,
            CorrelationId = e.CorrelationId,
            Status = (OutboxMessageStatus)e.Status,
            CreatedAt = e.CreatedAt,
            ProcessedAt = e.ProcessedAt,
            RetryCount = e.RetryCount,
            Error = e.Error
        }).ToList();
    }

    public async Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, (int)OutboxMessageStatus.Processed)
                    .SetProperty(x => x.ProcessedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, (int)OutboxMessageStatus.Failed)
                    .SetProperty(x => x.Error, error)
                    .SetProperty(x => x.RetryCount, x => x.RetryCount + 1),
                cancellationToken);
    }

    public async Task DeleteProcessedMessagesAsync(
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);

        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Status == (int)OutboxMessageStatus.Processed && x.ProcessedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
