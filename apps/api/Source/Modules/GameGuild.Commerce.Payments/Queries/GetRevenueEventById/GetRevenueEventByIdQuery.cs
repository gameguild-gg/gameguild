using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get revenue event by ID
/// </summary>
public record GetRevenueEventByIdQuery(Guid EventId) : IQuery<RevenueEvent?>;
