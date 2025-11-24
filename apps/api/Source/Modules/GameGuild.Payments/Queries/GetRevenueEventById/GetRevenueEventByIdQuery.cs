using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get revenue event by ID
/// </summary>
public record GetRevenueEventByIdQuery(Guid EventId) : IQuery<RevenueEvent?>;
