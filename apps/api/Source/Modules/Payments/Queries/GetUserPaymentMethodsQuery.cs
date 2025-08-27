namespace GameGuild.Modules.Payments;

/// <summary>
/// Query to get user payment methods
/// </summary>
public record GetUserPaymentMethodsQuery : IRequest<IEnumerable<UserFinancialMethod>> {
  public Guid? UserId { get; init; }
}
