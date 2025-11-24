using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Abstractions;

/// <summary>
///     Repository for revenue events
/// </summary>
public interface IRevenueEventRepository
{
    /// <summary>Get revenue event by ID</summary>
    Task<RevenueEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Get revenue events by date range</summary>
    Task<List<RevenueEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Get revenue events by reference ID</summary>
    Task<List<RevenueEvent>> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);

    /// <summary>Add new revenue event</summary>
    Task AddAsync(RevenueEvent revenueEvent, CancellationToken cancellationToken = default);

    /// <summary>Update revenue event</summary>
    Task UpdateAsync(RevenueEvent revenueEvent, CancellationToken cancellationToken = default);

    /// <summary>Save changes to database</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
