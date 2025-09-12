namespace GameGuild.CQRS;

/// <summary>
/// Repository for managing domain events
/// </summary>
public interface IDomainEventRepository
{
    /// <summary>
    /// Gets entities with pending domain events
    /// </summary>
    Task<IReadOnlyList<IHasDomainEvents>> GetEntitiesWithPendingEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
