using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get dispute by ID
/// </summary>
public record GetDisputeByIdQuery(Guid DisputeId) : IQuery<PaymentDispute?>;
