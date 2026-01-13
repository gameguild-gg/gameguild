namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Represents proration calculation for subscription plan changes.
///     This ensures upgrades/downgrades leave no financial residue.
/// </summary>
public record PlanChangeProration(
    /// <summary>
    ///     Credit for unused time on the old plan
    /// </summary>
    decimal CreditForUnused,

    /// <summary>
    ///     Charge for remaining time on the new plan
    /// </summary>
    decimal ChargeForNew,

    /// <summary>
    ///     Net adjustment (positive = customer owes, negative = credit due)
    /// </summary>
    decimal NetAdjustment,

    /// <summary>
    ///     When the proration takes effect
    /// </summary>
    DateTime EffectiveDate
)
{
    /// <summary>
    ///     Whether the customer should receive a credit
    /// </summary>
    public bool IsCredit => NetAdjustment < 0;

    /// <summary>
    ///     Whether the customer should be charged
    /// </summary>
    public bool IsCharge => NetAdjustment > 0;

    /// <summary>
    ///     Absolute value of the adjustment
    /// </summary>
    public decimal AbsoluteAdjustment => Math.Abs(NetAdjustment);
}
