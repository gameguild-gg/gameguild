namespace GameGuild.Modules.Payments.Models;

/// <summary> Types of discounts that can be applied </summary>
public enum DiscountType {
  /// <summary> Fixed amount discount </summary>
  FixedAmount,

  /// <summary> Percentage-based discount </summary>
  Percentage,

  /// <summary> Buy one get one free </summary>
  BuyOneGetOne,

  /// <summary> Free trial period </summary>
  FreeTrial,

  /// <summary> Volume-based discount </summary>
  VolumeDiscount,
}

/// <summary> Represents a discount that has been applied to a payment </summary>
public class AppliedDiscount {
  /// <summary> Unique identifier for this discount application </summary>
  public Guid Id { get; init; } = Guid.NewGuid();

  /// <summary> Discount code or identifier </summary>
  public string Code { get; init; } = string.Empty;

  /// <summary> Human-readable name/description </summary>
  public string Name { get; init; } = string.Empty;

  /// <summary> Type of discount </summary>
  public DiscountType Type { get; init; }

  /// <summary> Discount value (amount for fixed, percentage for percentage) </summary>
  public decimal Value { get; init; }

  /// <summary> Actual amount discounted </summary>
  public Money Amount { get; init; } = Money.Zero();

  /// <summary> Minimum purchase amount for discount to apply </summary>
  public Money? MinimumAmount { get; init; }

  /// <summary> Maximum discount amount (for percentage discounts) </summary>
  public Money? MaximumAmount { get; init; }

  /// <summary> When this discount was applied </summary>
  public DateTime AppliedAt { get; init; } = DateTime.UtcNow;

  /// <summary> Whether this discount was automatically applied </summary>
  public bool AutoApplied { get; init; }

  /// <summary> Source of the discount (coupon, loyalty program, etc.) </summary>
  public string? Source { get; init; }

  /// <summary> Additional metadata about the discount </summary>
  public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

  /// <summary> Create a fixed amount discount </summary>
  public static AppliedDiscount FixedAmount(string code, string name, Money amount, bool autoApplied = false) {
    return new AppliedDiscount { Code = code, Name = name, Type = DiscountType.FixedAmount, Value = amount.Amount, Amount = amount, AutoApplied = autoApplied };
  }

  /// <summary> Create a percentage discount </summary>
  public static AppliedDiscount Percentage(string code, string name, decimal percentage, Money calculatedAmount, Money? maximumAmount = null, bool autoApplied = false) {
    return new AppliedDiscount { Code = code, Name = name, Type = DiscountType.Percentage, Value = percentage, Amount = calculatedAmount, MaximumAmount = maximumAmount, AutoApplied = autoApplied };
  }
}
