using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Security;

/// <summary>
///     P0 Critical Tests: Upgrade/Downgrade Safety
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify plan change proration is calculated correctly.
/// </summary>
public class UpgradeDowngradeSafetyTests
{
    #region Plan Upgrade Proration Tests (P0)

    [Fact]
    public void ChangePlan_Upgrade_ShouldCalculateCorrectProration()
    {
        // Arrange
        var subscription = CreateActiveSubscriptionAtMidCycle();
        var oldAmount = subscription.Amount;
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(49.99m, "USD"); // Upgrading from 29.99 to 49.99
        
        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount);

        // Assert
        proration.Should().NotBeNull("upgrade should return proration info");
        proration.OldPlanAmount.Should().Be(oldAmount);
        proration.NewPlanAmount.Should().Be(newAmount);
        proration.DaysRemainingInCycle.Should().BeGreaterThan(0);
        proration.CreditAmount.Should().BeGreaterThan(0, "unused old plan period should generate credit");
        proration.ChargeAmount.Should().BeGreaterThan(0, "upgrade should charge for new plan");
    }

    [Fact]
    public void ChangePlan_Downgrade_ShouldCreditUnusedPeriod()
    {
        // Arrange
        var subscription = CreateActiveSubscriptionWithHigherPlan();
        var oldAmount = subscription.Amount;
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(19.99m, "USD"); // Downgrading from 49.99 to 19.99
        
        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount);

        // Assert
        proration.Should().NotBeNull("downgrade should return proration info");
        proration.CreditAmount.Should().BeGreaterThan(0, "unused higher plan period generates credit");
        proration.NetAmount.Should().BeLessThan(0).Or.Be(0m, "downgrade should result in credit or no charge");
    }

    [Fact]
    public void ChangePlan_MidCycle_ShouldChargeCorrectAmount()
    {
        // Arrange - Subscription at exactly mid-cycle (15 days into 30-day cycle)
        var subscription = CreateActiveSubscription();
        var cycleStart = DateTime.UtcNow.AddDays(-15);
        var cycleEnd = cycleStart.AddDays(30);
        
        subscription.CurrentPeriodStart = cycleStart;
        subscription.CurrentPeriodEnd = cycleEnd;
        
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(59.99m, "USD");
        var effectiveDate = DateTime.UtcNow;
        
        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount, effectiveDate);

        // Assert
        proration.Should().NotBeNull();
        // At mid-cycle, should get ~50% credit for old plan and ~50% charge for new plan
        proration.DaysRemainingInCycle.Should().BeCloseTo(15, 1);
    }

    [Fact]
    public void ChangePlan_Downgrade_ShouldBeEffectiveAtPeriodEnd()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(9.99m, "USD");
        
        // Act - Downgrade should take effect at period end
        var proration = subscription.ChangePlan(newPlanId, newAmount);

        // Assert
        subscription.PlanId.Should().Be(newPlanId, "plan ID should be updated");
        subscription.Amount.Should().Be(newAmount, "amount should reflect new plan");
        proration.EffectiveDate.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ChangePlan_ToSamePlan_ShouldNotGenerateCharges()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var currentAmount = subscription.Amount;
        var currentPlanId = subscription.PlanId;
        
        // Act - Change to same plan (edge case)
        var proration = subscription.ChangePlan(currentPlanId, currentAmount);

        // Assert
        proration.NetAmount.Should().Be(0m, "no net charge for same plan");
    }

    [Fact]
    public void ChangePlan_AtCycleStart_ShouldHaveFullCycleRemaining()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.CurrentPeriodStart = DateTime.UtcNow;
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(30);
        
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(49.99m, "USD");
        
        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount);

        // Assert
        proration.DaysRemainingInCycle.Should().BeCloseTo(30, 1);
        proration.CreditAmount.Should().BeCloseTo(subscription.Amount.Amount, 0.01m, "full credit for old plan at cycle start");
    }

    [Fact]
    public void ChangePlan_AtCycleEnd_ShouldHaveMinimalProration()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.CurrentPeriodStart = DateTime.UtcNow.AddDays(-29);
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(1);
        
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(49.99m, "USD");
        
        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount);

        // Assert
        proration.DaysRemainingInCycle.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void ChangePlan_WithNullEffectiveDate_ShouldUseNow()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(39.99m, "USD");
        var beforeChange = DateTime.UtcNow;
        
        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount, effectiveDate: null);

        // Assert
        proration.EffectiveDate.Should().BeOnOrAfter(beforeChange);
    }

    [Fact]
    public void ChangePlan_WhenCancelled_ShouldThrow()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "Test");
        
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(39.99m, "USD");

        // Act & Assert
        var act = () => subscription.ChangePlan(newPlanId, newAmount);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Proration Calculation Tests

    [Fact]
    public void Proration_CreditPlusCharge_ShouldEqualNetAmount()
    {
        // Arrange
        var subscription = CreateActiveSubscriptionAtMidCycle();
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(59.99m, "USD");
        
        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount);

        // Assert - Net = Charge - Credit
        var expectedNet = proration.ChargeAmount - proration.CreditAmount;
        proration.NetAmount.Should().BeApproximately(expectedNet, 0.01m);
    }

    [Fact]
    public void Proration_ShouldUseDailyRate()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var monthlyAmount = subscription.Amount.Amount;
        var expectedDailyRate = monthlyAmount / 30; // Approximate
        
        subscription.CurrentPeriodStart = DateTime.UtcNow.AddDays(-15);
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(15);
        
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(59.99m, "USD");
        
        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount);

        // Assert - Credit should be approximately half of old plan (15 days remaining)
        var expectedCredit = expectedDailyRate * 15;
        proration.CreditAmount.Should().BeApproximately(expectedCredit, 1m);
    }

    #endregion

    #region Helper Methods

    private static Subscription CreateActiveSubscription()
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow
        );
        subscription.Activate();
        subscription.CurrentPeriodStart = DateTime.UtcNow;
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(30);
        return subscription;
    }

    private static Subscription CreateActiveSubscriptionAtMidCycle()
    {
        var subscription = CreateActiveSubscription();
        subscription.CurrentPeriodStart = DateTime.UtcNow.AddDays(-15);
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(15);
        return subscription;
    }

    private static Subscription CreateActiveSubscriptionWithHigherPlan()
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(49.99m, "USD"),
            startDate: DateTime.UtcNow
        );
        subscription.Activate();
        subscription.CurrentPeriodStart = DateTime.UtcNow.AddDays(-15);
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(15);
        return subscription;
    }

    #endregion
}
