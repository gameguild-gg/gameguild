namespace GameGuild.Modules.Programs.DTOs;

public record UpdatePricingDto(
  decimal? Price = null,
  string? Currency = null,
  bool? IsSubscription = null,
  int? SubscriptionDurationDays = null
) {
  public decimal? Price { get; init; } = Price;

  public string? Currency { get; init; } = Currency;

  public bool? IsSubscription { get; init; } = IsSubscription;

  public int? SubscriptionDurationDays { get; init; } = SubscriptionDurationDays;
}
