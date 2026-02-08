
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Extensions for BillingCycle enum
/// </summary>
public static class BillingCycleExtensions
{
    /// <summary>
    ///     Gets the number of months for the billing cycle
    /// </summary>
    public static int GetMonths(this BillingCycle cycle) { return (int) cycle; }

    /// <summary>
    ///     Gets the display name for the billing cycle
    /// </summary>
    public static string GetDisplayName(this BillingCycle cycle)
    {
        return cycle switch
        {
            BillingCycle.Monthly => "Monthly",
            BillingCycle.Quarterly => "Quarterly",
            BillingCycle.SemiAnnually => "Semi-Annually",
            BillingCycle.Annually => "Annually",
            BillingCycle.Biannually => "Biannually",
            _ => cycle.ToString()
        };
    }

    /// <summary>
    ///     Gets the frequency description
    /// </summary>
    public static string GetFrequencyDescription(this BillingCycle cycle)
    {
        return cycle switch
        {
            BillingCycle.Monthly => "Every month",
            BillingCycle.Quarterly => "Every 3 months",
            BillingCycle.SemiAnnually => "Every 6 months",
            BillingCycle.Annually => "Every year",
            BillingCycle.Biannually => "Every 2 years",
            _ => $"Every {cycle.GetMonths()} months"
        };
    }

    /// <summary>
    ///     Calculates the next billing date from a start date
    /// </summary>
    public static DateTime CalculateNextBillingDate(this BillingCycle cycle, DateTime startDate)
    {
        return cycle switch
        {
            BillingCycle.Monthly => startDate.AddMonths(1),
            BillingCycle.Quarterly => startDate.AddMonths(3),
            BillingCycle.SemiAnnually => startDate.AddMonths(6),
            BillingCycle.Annually => startDate.AddYears(1),
            BillingCycle.Biannually => startDate.AddYears(2),
            _ => startDate.AddMonths(cycle.GetMonths())
        };
    }
}
