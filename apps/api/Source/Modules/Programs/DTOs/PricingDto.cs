namespace GameGuild.Modules.Programs.DTOs;

public record PricingDto(
  decimal Price,
  string Currency,
  bool IsSubscription,
  int? SubscriptionDurationDays,
  bool IsMonetizationEnabled
) {
  public decimal Price { get; init; } = Price;

  public string Currency { get; init; } = Currency;

  public bool IsSubscription { get; init; } = IsSubscription;

  public int? SubscriptionDurationDays { get; init; } = SubscriptionDurationDays;

  public bool IsMonetizationEnabled { get; init; } = IsMonetizationEnabled;
}
