using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

/// <summary>
/// Unit tests for BillingCalculator service
/// </summary>
public class BillingCalculatorTests
{
    private readonly BillingCalculator _calculator = new();

    #region CalculateBillingPeriod Tests

    [Fact]
    public void CalculateBillingPeriod_Weekly_ShouldReturn7DayPeriod()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateBillingPeriod(startDate, BillingCycle.Weekly);

        // Assert
        result.PeriodStart.Should().Be(startDate);
        result.PeriodEnd.Should().Be(new DateTime(2026, 1, 7));
        result.NextBillingDate.Should().Be(new DateTime(2026, 1, 8));
    }

    [Fact]
    public void CalculateBillingPeriod_Monthly_ShouldReturn1MonthPeriod()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 15);

        // Act
        var result = _calculator.CalculateBillingPeriod(startDate, BillingCycle.Monthly);

        // Assert
        result.PeriodStart.Should().Be(startDate);
        result.PeriodEnd.Should().Be(new DateTime(2026, 2, 14));
        result.NextBillingDate.Should().Be(new DateTime(2026, 2, 15));
    }

    [Fact]
    public void CalculateBillingPeriod_Quarterly_ShouldReturn3MonthPeriod()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateBillingPeriod(startDate, BillingCycle.Quarterly);

        // Assert
        result.PeriodStart.Should().Be(startDate);
        result.PeriodEnd.Should().Be(new DateTime(2026, 3, 31));
        result.NextBillingDate.Should().Be(new DateTime(2026, 4, 1));
    }

    [Fact]
    public void CalculateBillingPeriod_SemiAnnually_ShouldReturn6MonthPeriod()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateBillingPeriod(startDate, BillingCycle.SemiAnnually);

        // Assert
        result.PeriodStart.Should().Be(startDate);
        result.PeriodEnd.Should().Be(new DateTime(2026, 6, 30));
        result.NextBillingDate.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void CalculateBillingPeriod_Annually_ShouldReturn1YearPeriod()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateBillingPeriod(startDate, BillingCycle.Annually);

        // Assert
        result.PeriodStart.Should().Be(startDate);
        result.PeriodEnd.Should().Be(new DateTime(2026, 12, 31));
        result.NextBillingDate.Should().Be(new DateTime(2027, 1, 1));
    }

    [Fact]
    public void CalculateBillingPeriod_Biannually_ShouldReturn2YearPeriod()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateBillingPeriod(startDate, BillingCycle.Biannually);

        // Assert
        result.PeriodStart.Should().Be(startDate);
        result.PeriodEnd.Should().Be(new DateTime(2027, 12, 31));
        result.NextBillingDate.Should().Be(new DateTime(2028, 1, 1));
    }

    [Fact]
    public void CalculateBillingPeriod_UnsupportedCycle_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var invalidCycle = (BillingCycle)999;

        // Act
        var act = () => _calculator.CalculateBillingPeriod(startDate, invalidCycle);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("billingCycle");
    }

    [Fact]
    public void CalculateBillingPeriod_LeapYear_ShouldHandleFebruaryCorrectly()
    {
        // 2028 is a leap year
        var startDate = new DateTime(2028, 2, 29);

        // Act
        var result = _calculator.CalculateBillingPeriod(startDate, BillingCycle.Monthly);

        // Assert
        result.PeriodStart.Should().Be(startDate);
        result.NextBillingDate.Should().Be(new DateTime(2028, 3, 29));
    }

    #endregion

    #region CalculateNextBillingDate Tests

    [Theory]
    [InlineData(BillingCycle.Weekly, 7)]
    public void CalculateNextBillingDate_Weekly_ShouldAdd7Days(BillingCycle cycle, int expectedDays)
    {
        // Arrange
        var currentDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateNextBillingDate(currentDate, cycle);

        // Assert
        result.Should().Be(currentDate.AddDays(expectedDays));
    }

    [Fact]
    public void CalculateNextBillingDate_Monthly_ShouldAdd1Month()
    {
        // Arrange
        var currentDate = new DateTime(2026, 1, 15);

        // Act
        var result = _calculator.CalculateNextBillingDate(currentDate, BillingCycle.Monthly);

        // Assert
        result.Should().Be(new DateTime(2026, 2, 15));
    }

    [Fact]
    public void CalculateNextBillingDate_Quarterly_ShouldAdd3Months()
    {
        // Arrange
        var currentDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateNextBillingDate(currentDate, BillingCycle.Quarterly);

        // Assert
        result.Should().Be(new DateTime(2026, 4, 1));
    }

    [Fact]
    public void CalculateNextBillingDate_SemiAnnually_ShouldAdd6Months()
    {
        // Arrange
        var currentDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateNextBillingDate(currentDate, BillingCycle.SemiAnnually);

        // Assert
        result.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void CalculateNextBillingDate_Annually_ShouldAdd1Year()
    {
        // Arrange
        var currentDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateNextBillingDate(currentDate, BillingCycle.Annually);

        // Assert
        result.Should().Be(new DateTime(2027, 1, 1));
    }

    [Fact]
    public void CalculateNextBillingDate_Biannually_ShouldAdd2Years()
    {
        // Arrange
        var currentDate = new DateTime(2026, 1, 1);

        // Act
        var result = _calculator.CalculateNextBillingDate(currentDate, BillingCycle.Biannually);

        // Assert
        result.Should().Be(new DateTime(2028, 1, 1));
    }

    [Fact]
    public void CalculateNextBillingDate_UnsupportedCycle_ShouldDefaultToMonthly()
    {
        // Arrange
        var currentDate = new DateTime(2026, 1, 1);
        var invalidCycle = (BillingCycle)999;

        // Act
        var result = _calculator.CalculateNextBillingDate(currentDate, invalidCycle);

        // Assert - Falls back to monthly
        result.Should().Be(new DateTime(2026, 2, 1));
    }

    #endregion

    #region CalculateProration Tests

    [Fact]
    public void CalculateProration_Upgrade_ShouldCalculatePositiveNetAdjustment()
    {
        // Arrange
        var oldAmount = new Money(100m, "USD");
        var newAmount = new Money(200m, "USD");
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);
        var effectiveDate = new DateTime(2026, 1, 16); // 15 days remaining

        // Act
        var result = _calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, effectiveDate);

        // Assert
        result.EffectiveDate.Should().Be(effectiveDate);
        result.CreditForUnused.Should().BeGreaterThan(0);
        result.ChargeForNew.Should().BeGreaterThan(result.CreditForUnused);
        result.NetAdjustment.Should().BeGreaterThan(0); // Upgrade = positive adjustment
    }

    [Fact]
    public void CalculateProration_Downgrade_ShouldCalculateNegativeNetAdjustment()
    {
        // Arrange
        var oldAmount = new Money(200m, "USD");
        var newAmount = new Money(100m, "USD");
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);
        var effectiveDate = new DateTime(2026, 1, 16);

        // Act
        var result = _calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, effectiveDate);

        // Assert
        result.NetAdjustment.Should().BeLessThan(0); // Downgrade = negative adjustment (credit)
    }

    [Fact]
    public void CalculateProration_SamePlan_ShouldCalculateZeroNetAdjustment()
    {
        // Arrange
        var amount = new Money(100m, "USD");
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);
        var effectiveDate = new DateTime(2026, 1, 16);

        // Act
        var result = _calculator.CalculateProration(amount, amount, periodStart, periodEnd, effectiveDate);

        // Assert
        result.NetAdjustment.Should().Be(0);
        result.CreditForUnused.Should().Be(result.ChargeForNew);
    }

    [Fact]
    public void CalculateProration_AtPeriodEnd_ShouldReturnZeroAdjustment()
    {
        // Arrange
        var oldAmount = new Money(100m, "USD");
        var newAmount = new Money(200m, "USD");
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);
        var effectiveDate = new DateTime(2026, 2, 1); // After period end

        // Act
        var result = _calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, effectiveDate);

        // Assert
        result.CreditForUnused.Should().Be(0);
        result.ChargeForNew.Should().Be(0);
        result.NetAdjustment.Should().Be(0);
    }

    [Fact]
    public void CalculateProration_InvalidPeriod_ShouldReturnZeroAdjustment()
    {
        // Arrange - period end before period start
        var oldAmount = new Money(100m, "USD");
        var newAmount = new Money(200m, "USD");
        var periodStart = new DateTime(2026, 1, 31);
        var periodEnd = new DateTime(2026, 1, 1);
        var effectiveDate = new DateTime(2026, 1, 15);

        // Act
        var result = _calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, effectiveDate);

        // Assert
        result.NetAdjustment.Should().Be(0);
    }

    [Fact]
    public void CalculateProration_MidMonthChange_ShouldCalculateCorrectly()
    {
        // Arrange
        var oldAmount = new Money(29m, "USD"); // Period is 29 days (Jan 1-30), so $1/day exactly
        var newAmount = new Money(58m, "USD"); // $2/day
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 30); // 29 day period (Jan 1 to Jan 30)
        var effectiveDate = new DateTime(2026, 1, 16); // 14 days remaining (Jan 16-30)

        // Act
        var result = _calculator.CalculateProration(oldAmount, newAmount, periodStart, periodEnd, effectiveDate);

        // Assert
        // Period = 29 days, remaining = 14 days
        // Daily rate old = 29/29 = 1
        // Daily rate new = 58/29 = 2
        // Credit: 14 days * $1/day = $14
        // Charge: 14 days * $2/day = $28
        // Net: $28 - $14 = $14
        result.CreditForUnused.Should().BeApproximately(14m, 0.01m);
        result.ChargeForNew.Should().BeApproximately(28m, 0.01m);
        result.NetAdjustment.Should().BeApproximately(14m, 0.01m);
    }

    #endregion

    #region CalculateTrialEndDate Tests

    [Fact]
    public void CalculateTrialEndDate_ValidDays_ShouldReturnCorrectEndDate()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var trialDays = 14;

        // Act
        var result = _calculator.CalculateTrialEndDate(startDate, trialDays);

        // Assert
        result.Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public void CalculateTrialEndDate_ZeroDays_ShouldReturnStartDate()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var trialDays = 0;

        // Act
        var result = _calculator.CalculateTrialEndDate(startDate, trialDays);

        // Assert
        result.Should().Be(startDate);
    }

    [Fact]
    public void CalculateTrialEndDate_NegativeDays_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var trialDays = -5;

        // Act
        var act = () => _calculator.CalculateTrialEndDate(startDate, trialDays);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("trialDays");
    }

    [Fact]
    public void CalculateTrialEndDate_30DayTrial_ShouldHandleMonthBoundary()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 15);
        var trialDays = 30;

        // Act
        var result = _calculator.CalculateTrialEndDate(startDate, trialDays);

        // Assert
        result.Should().Be(new DateTime(2026, 2, 14));
    }

    #endregion

    #region GetDaysRemainingInPeriod Tests

    [Fact]
    public void GetDaysRemainingInPeriod_FuturePeriodEnd_ShouldReturnPositiveDays()
    {
        // Arrange
        var periodEnd = DateTime.UtcNow.AddDays(10);

        // Act
        var result = _calculator.GetDaysRemainingInPeriod(periodEnd);

        // Assert
        result.Should().BeGreaterOrEqualTo(9); // At least 9 due to timing
        result.Should().BeLessOrEqualTo(11);
    }

    [Fact]
    public void GetDaysRemainingInPeriod_PastPeriodEnd_ShouldReturnZero()
    {
        // Arrange
        var periodEnd = DateTime.UtcNow.AddDays(-5);

        // Act
        var result = _calculator.GetDaysRemainingInPeriod(periodEnd);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetDaysRemainingInPeriod_TodayPeriodEnd_ShouldReturnZeroOrOne()
    {
        // Arrange
        var periodEnd = DateTime.UtcNow;

        // Act
        var result = _calculator.GetDaysRemainingInPeriod(periodEnd);

        // Assert
        result.Should().BeGreaterOrEqualTo(0);
        result.Should().BeLessOrEqualTo(1);
    }

    #endregion

    #region GetRemainingTrialDays Tests

    [Fact]
    public void GetRemainingTrialDays_NullTrialEndDate_ShouldReturnNull()
    {
        // Arrange
        DateTime? trialEndDate = null;

        // Act
        var result = _calculator.GetRemainingTrialDays(trialEndDate);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRemainingTrialDays_FutureTrialEnd_ShouldReturnPositiveDays()
    {
        // Arrange
        var trialEndDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = _calculator.GetRemainingTrialDays(trialEndDate);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().BeGreaterOrEqualTo(6);
        result.Value.Should().BeLessOrEqualTo(8);
    }

    [Fact]
    public void GetRemainingTrialDays_PastTrialEnd_ShouldReturnZero()
    {
        // Arrange
        var trialEndDate = DateTime.UtcNow.AddDays(-3);

        // Act
        var result = _calculator.GetRemainingTrialDays(trialEndDate);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(0);
    }

    #endregion
}
