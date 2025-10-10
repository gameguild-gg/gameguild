using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Query to get disputes by user ID</summary>
public record GetDisputesByUserIdQuery(
    Guid UserId,
    int Skip = 0,
    int Take = 50
) : IRequest<List<PaymentDispute>>;
