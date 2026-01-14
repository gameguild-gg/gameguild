namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Represents proration calculation for subscription plan changes.
///     This ensures upgrades/downgrades leave no financial residue.
/// </summary>
/// <param name="CreditForUnused">Credit for unused time on the old plan</param>
/// <param name="ChargeForNew">Charge for remaining time on the new plan</param>
/// <param name="NetAdjustment">Net adjustment (positive = customer owes, negative = credit due)</param>
/// <param name="EffectiveDate">When the proration takes effect</param>
public record PlanChangeProration(
    decimal CreditForUnused,
    decimal ChargeForNew,
    decimal NetAdjustment,
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
