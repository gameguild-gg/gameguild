namespace GameGuild;

/// <summary> Represents a money value with currency </summary>
public record Money {
  // Private parameterless constructor for EF Core
  private Money() {
    Amount = 0;
    Currency = "USD";
  }

  public Money(decimal amount, string currency = "USD") {
    if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));

    if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));

    Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    Currency = currency.ToUpperInvariant();
  }

  public decimal Amount { get; init; }

  public string Currency { get; init; }

  public static Money Zero(string currency = "USD") { return new Money(0, currency); }

  /// <summary> Creates a Money instance from a decimal amount with USD currency </summary>
  public static Money FromDecimal(decimal amount) { return new Money(amount, "USD"); }

  public static Money operator +(Money left, Money right) {
    return left.Currency != right.Currency ? throw new InvalidOperationException("Cannot add money with different currencies.") : new Money(left.Amount + right.Amount, left.Currency);
  }

  public static Money operator -(Money left, Money right) {
    return left.Currency != right.Currency ? throw new InvalidOperationException("Cannot subtract money with different currencies.") : new Money(left.Amount - right.Amount, left.Currency);
  }

  public static Money operator *(Money money, decimal multiplier) { return new Money(money.Amount * multiplier, money.Currency); }

  public static Money operator /(Money money, decimal divisor) {
    return divisor == 0 ? throw new DivideByZeroException("Cannot divide money by zero.") : new Money(money.Amount / divisor, money.Currency);
  }

  public static bool operator >(Money left, Money right) {
    if (left.Currency != right.Currency) throw new InvalidOperationException("Cannot compare money with different currencies.");

    return left.Amount > right.Amount;
  }

  public static bool operator <(Money left, Money right) {
    if (left.Currency != right.Currency) throw new InvalidOperationException("Cannot compare money with different currencies.");

    return left.Amount < right.Amount;
  }

  public static bool operator >=(Money left, Money right) { return !(left < right); }

  public static bool operator <=(Money left, Money right) { return !(left > right); }

  // Explicit conversion operators to prevent ambiguous conversions
  public static explicit operator Money(decimal amount) { return new Money(amount); }

  public static explicit operator decimal(Money money) { return money.Amount; }

  // Factory methods for cleaner conversion syntax
  public static Money FromDecimal(decimal amount, string currency = "USD") { return new Money(amount, currency); }

  public decimal ToDecimal() { return Amount; }

  public override string ToString() { return $"{Amount:C} {Currency}"; }
}
