namespace GameGuild;

/// <summary>
///     Represents a money value with currency
/// </summary>
public record Money
{
    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));

        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency = "USD") { return new Money(0, currency); }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies.");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier) { return new Money(money.Amount * multiplier, money.Currency); }

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0) throw new DivideByZeroException("Cannot divide money by zero.");

        return new Money(money.Amount / divisor, money.Currency);
    }

    public static bool operator >(Money left, Money right)
    {
        if (left.Currency != right.Currency) throw new InvalidOperationException("Cannot compare money with different currencies.");

        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        if (left.Currency != right.Currency) throw new InvalidOperationException("Cannot compare money with different currencies.");

        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right) { return !(left < right); }

    public static bool operator <=(Money left, Money right) { return !(left > right); }

    public override string ToString() { return $"{Amount:C} {Currency}"; }
}
