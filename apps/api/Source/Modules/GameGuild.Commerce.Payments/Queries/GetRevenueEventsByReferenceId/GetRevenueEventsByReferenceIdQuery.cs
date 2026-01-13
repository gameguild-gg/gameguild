using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get revenue events by reference ID
/// </summary>
public record GetRevenueEventsByReferenceIdQuery(string ReferenceId) : IQuery<List<RevenueEvent>>;
