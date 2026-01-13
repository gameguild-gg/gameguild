using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get dispute by ID
/// </summary>
public record GetDisputeByIdQuery(Guid DisputeId) : IQuery<PaymentDispute?>;
