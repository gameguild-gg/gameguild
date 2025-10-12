using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Features.QueryDisputes;

/// <summary>Query to get dispute by ID</summary>
public record GetDisputeByIdQuery(
    Guid DisputeId
) : IRequest<PaymentDispute?>;
