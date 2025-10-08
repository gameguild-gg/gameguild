using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.CQRS;

public class DomainEventRepository(ApplicationDbContext dbContext) : IDomainEventRepository
{
    public Task<IReadOnlyList<IHasDomainEvents>> GetEntitiesWithPendingEventsAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = dbContext.ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is IHasDomainEvents entity && entity.DomainEvents.Any())
            .Select(entry => (IHasDomainEvents)entry.Entity)
            .ToList();

        return Task.FromResult<IReadOnlyList<IHasDomainEvents>>(entitiesWithEvents.AsReadOnly());
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
