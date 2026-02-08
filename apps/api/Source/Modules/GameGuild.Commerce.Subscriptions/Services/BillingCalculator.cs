
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Service for calculating billing-related dates and proration.
///     Extracted from Subscription entity to improve single responsibility.
/// </summary>
public interface IBillingCalculator
{
    /// <summary>
    ///     Calculates billing period dates based on start date and billing cycle.
    /// </summary>
    /// <param name="startDate">The start date of the billing period</param>
    /// <param name="billingCycle">The billing cycle frequency</param>
    /// <returns>Tuple containing period start, period end, and next billing date</returns>
    BillingPeriod CalculateBillingPeriod(DateTime startDate, BillingCycle billingCycle);

    /// <summary>
    ///     Calculates the next billing date based on current date and billing cycle.
    /// </summary>
    /// <param name="currentDate">The current date (typically the payment date)</param>
    /// <param name="billingCycle">The billing cycle frequency</param>
    /// <returns>The next billing date</returns>
    DateTime CalculateNextBillingDate(DateTime currentDate, BillingCycle billingCycle);

    /// <summary>
    ///     Calculates proration for plan changes.
    /// </summary>
    /// <param name="oldAmount">The original subscription amount</param>
    /// <param name="newAmount">The new subscription amount</param>
    /// <param name="periodStart">The current billing period start date</param>
    /// <param name="periodEnd">The current billing period end date</param>
    /// <param name="effectiveDate">When the change takes effect</param>
    /// <returns>Proration details including credit, charge, and net adjustment</returns>
    PlanChangeProration CalculateProration(
        Money oldAmount,
        Money newAmount,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime effectiveDate);

    /// <summary>
    ///     Calculates the trial end date.
    /// </summary>
    /// <param name="startDate">The trial start date</param>
    /// <param name="trialDays">Number of trial days</param>
    /// <returns>The trial end date</returns>
    DateTime CalculateTrialEndDate(DateTime startDate, int trialDays);

    /// <summary>
    ///     Gets remaining days in the current billing period.
    /// </summary>
    /// <param name="periodEnd">The current period end date</param>
    /// <returns>Number of days remaining</returns>
    int GetDaysRemainingInPeriod(DateTime periodEnd);

    /// <summary>
    ///     Gets remaining trial days.
    /// </summary>
    /// <param name="trialEndDate">The trial end date</param>
    /// <returns>Number of trial days remaining, or null if not in trial</returns>
    int? GetRemainingTrialDays(DateTime? trialEndDate);
}

/// <summary>
///     Represents a billing period with start, end, and next billing dates.
/// </summary>
/// <param name="PeriodStart">Start of the current billing period</param>
/// <param name="PeriodEnd">End of the current billing period</param>
/// <param name="NextBillingDate">Date of the next billing</param>
public readonly record struct BillingPeriod(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime NextBillingDate);

/// <summary>
///     Default implementation of billing calculator.
/// </summary>
public class BillingCalculator : IBillingCalculator
{
    /// <inheritdoc />
    public BillingPeriod CalculateBillingPeriod(DateTime startDate, BillingCycle billingCycle)
    {
        var periodStart = startDate;

        var (periodEnd, nextBilling) = billingCycle switch
        {
            BillingCycle.Weekly => (startDate.AddDays(7).AddDays(-1), startDate.AddDays(7)),
            BillingCycle.Monthly => (startDate.AddMonths(1).AddDays(-1), startDate.AddMonths(1)),
            BillingCycle.Quarterly => (startDate.AddMonths(3).AddDays(-1), startDate.AddMonths(3)),
            BillingCycle.SemiAnnually => (startDate.AddMonths(6).AddDays(-1), startDate.AddMonths(6)),
            BillingCycle.Annually => (startDate.AddYears(1).AddDays(-1), startDate.AddYears(1)),
            BillingCycle.Biannually => (startDate.AddYears(2).AddDays(-1), startDate.AddYears(2)),
            _ => throw new ArgumentOutOfRangeException(nameof(billingCycle), billingCycle, "Unsupported billing cycle")
        };

        return new BillingPeriod(periodStart, periodEnd, nextBilling);
    }

    /// <inheritdoc />
    public DateTime CalculateNextBillingDate(DateTime currentDate, BillingCycle billingCycle)
    {
        return billingCycle switch
        {
            BillingCycle.Weekly => currentDate.AddDays(7),
            BillingCycle.Monthly => currentDate.AddMonths(1),
            BillingCycle.Quarterly => currentDate.AddMonths(3),
            BillingCycle.SemiAnnually => currentDate.AddMonths(6),
            BillingCycle.Annually => currentDate.AddYears(1),
            BillingCycle.Biannually => currentDate.AddYears(2),
            _ => currentDate.AddMonths(1) // Fallback to monthly
        };
    }

    /// <inheritdoc />
    public PlanChangeProration CalculateProration(
        Money oldAmount,
        Money newAmount,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime effectiveDate)
    {
        var totalDaysInPeriod = (periodEnd - periodStart).TotalDays;
        var remainingDays = Math.Max(0, (periodEnd - effectiveDate).TotalDays);

        if (totalDaysInPeriod <= 0 || remainingDays <= 0)
            return new PlanChangeProration(0, 0, 0, effectiveDate);

        var dailyRateOld = oldAmount.Amount / (decimal)totalDaysInPeriod;
        var dailyRateNew = newAmount.Amount / (decimal)totalDaysInPeriod;

        var creditForUnused = dailyRateOld * (decimal)remainingDays;
        var chargeForNew = dailyRateNew * (decimal)remainingDays;
        var netAdjustment = chargeForNew - creditForUnused;

        return new PlanChangeProration(creditForUnused, chargeForNew, netAdjustment, effectiveDate);
    }

    /// <inheritdoc />
    public DateTime CalculateTrialEndDate(DateTime startDate, int trialDays)
    {
        if (trialDays < 0)
            throw new ArgumentOutOfRangeException(nameof(trialDays), "Trial days cannot be negative");

        return startDate.AddDays(trialDays);
    }

    /// <inheritdoc />
    public int GetDaysRemainingInPeriod(DateTime periodEnd)
    {
        return Math.Max(0, (periodEnd - DateTime.UtcNow).Days);
    }

    /// <inheritdoc />
    public int? GetRemainingTrialDays(DateTime? trialEndDate)
    {
        if (!trialEndDate.HasValue)
            return null;

        var remaining = (trialEndDate.Value - DateTime.UtcNow).Days;
        return Math.Max(0, remaining);
    }
}
