using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Query to get disputes by payment ID</summary>
public record GetDisputesByPaymentIdQuery(
    Guid PaymentId
) : IRequest<List<PaymentDispute>>;
