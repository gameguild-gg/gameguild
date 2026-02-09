using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get revenue events by reference ID
/// </summary>
public sealed record GetRevenueEventsByReferenceIdQuery(string ReferenceId) : IQuery<List<RevenueEvent>>;
