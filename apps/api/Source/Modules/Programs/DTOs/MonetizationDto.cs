namespace GameGuild.Modules.Programs;

public record MonetizationDto(
  decimal Price,
  string Currency = "USD",
  bool IsSubscription = false,
  int? SubscriptionDurationDays = null
) {
  public decimal Price { get; init; } = Price;

  public string Currency { get; init; } = Currency;

  public bool IsSubscription { get; init; } = IsSubscription;

  public int? SubscriptionDurationDays { get; init; } = SubscriptionDurationDays;
}
