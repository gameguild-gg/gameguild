using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get revenue events by reference ID
/// </summary>
public record GetRevenueEventsByReferenceIdQuery(string ReferenceId) : IQuery<List<RevenueEvent>>;
