using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get revenue events by date range
/// </summary>
public record GetRevenueEventsByDateRangeQuery(DateTime StartDate, DateTime EndDate, int Skip = 0, int Take = 100) : IQuery<List<RevenueEvent>>;
