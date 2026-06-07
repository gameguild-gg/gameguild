using Microsoft.EntityFrameworkCore;

namespace GameGuild.AI;

internal sealed class AiConversationHistoryRepository(IApplicationDbContext db) : IAiConversationHistoryRepository
{
    public async Task AddAsync(AiConversationLog entry, CancellationToken cancellationToken = default)
    {
        db.Set<AiConversationLog>().Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AiConversationHistoryEntryDto>> GetRecentAsync(
        Guid tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);

        return await db.Set<AiConversationLog>()
            .AsNoTracking()
            .Where(log => log.TenantId == tenantId)
            .OrderByDescending(log => log.OccurredAt)
            .Take(take)
            .Select(log => new AiConversationHistoryEntryDto(
                log.Id,
                log.UserId,
                log.RequestKind,
                log.Provider,
                log.Model,
                log.RequestText,
                log.SystemPrompt,
                log.ResponseText,
                log.Outcome,
                log.OutcomeCode,
                log.OutcomeReason,
                log.FinishReason,
                new AiUsageDto(log.InputTokens, log.OutputTokens, log.TotalTokens),
                log.OccurredAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
