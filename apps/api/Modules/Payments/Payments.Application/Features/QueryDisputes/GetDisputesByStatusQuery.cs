using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Query to get disputes by status</summary>
public record GetDisputesByStatusQuery(
    DisputeStatus Status,
    int Skip = 0,
    int Take = 50
) : IRequest<List<PaymentDispute>>;
