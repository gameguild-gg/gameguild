using GameGuild.Modules.Common.Inbox.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Common.Inbox;

/// <summary>
/// Database-backed implementation of inbox service
/// </summary>
internal sealed class InboxService : IInboxService
{
    private readonly DbContext _dbContext;

    public InboxService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasBeenProcessedAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<InboxMessageEntity>()
            .AnyAsync(x => x.MessageId == messageId, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(
        string messageId,
        string messageType,
        DateTime receivedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = new InboxMessageEntity
        {
            MessageId = messageId,
            MessageType = messageType,
            ReceivedAt = receivedAt,
            ProcessedAt = DateTime.UtcNow
        };

        _dbContext.Set<InboxMessageEntity>().Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Duplicate key - message already processed, ignore
        }
    }

    public async Task DeleteProcessedMessagesAsync(
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);

        await _dbContext.Set<InboxMessageEntity>()
            .Where(x => x.ProcessedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
