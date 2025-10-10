using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Queries;

public record GetRevenueEventsByDateRangeQuery(
    DateTime StartDate,
    DateTime EndDate,
    int Skip = 0,
    int Take = 100
) : IRequest<List<RevenueEvent>>;
