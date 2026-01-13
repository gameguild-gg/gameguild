using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to create a payment dispute
/// </summary>
[RequiresQuota(ResourceUsageType.Disputes, 1, Source = "CreateDispute")]
public sealed record CreateDisputeCommand(Guid PaymentId, Guid UserId, DisputeType Type, decimal Amount, string Reason, string Description) : ICommand<PaymentDispute>;
