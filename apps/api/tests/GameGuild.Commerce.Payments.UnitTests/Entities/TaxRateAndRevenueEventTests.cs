using FluentAssertions;
using GameGuild.Commerce.Payments;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Entities;

/// <summary>
/// Unit tests for TaxRate entity
/// </summary>
public class TaxRateEntityTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var taxRate = new TaxRate();

        // Assert
        taxRate.IsActive.Should().BeTrue();
        taxRate.Rate.Should().Be(0);
        taxRate.ProductCategory.Should().BeNull();
        taxRate.MinimumTaxableAmount.Should().BeNull();
        taxRate.MaximumTaxableAmount.Should().BeNull();
    }

    [Theory]
    [InlineData(0.0825)] // 8.25%
    [InlineData(0.07)]   // 7%
    [InlineData(0.21)]   // 21%
    [InlineData(0.00)]   // Tax-free
    public void Rate_ShouldAcceptValidRates(decimal rate)
    {
        // Arrange
        var taxRate = new TaxRate();

        // Act
        taxRate.Rate = rate;

        // Assert
        taxRate.Rate.Should().Be(rate);
    }

    [Theory]
    [InlineData(TaxType.VAT)]
    [InlineData(TaxType.GST)]
    [InlineData(TaxType.SalesTax)]
    public void TaxType_ShouldAcceptAllTypes(TaxType type)
    {
        // Arrange
        var taxRate = new TaxRate();

        // Act
        taxRate.TaxType = type;

        // Assert
        taxRate.TaxType.Should().Be(type);
    }

    [Fact]
    public void IsEffective_WhenActiveAndWithinDateRange_ShouldReturnTrue()
    {
        // Arrange
        var taxRate = new TaxRate
        {
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            EffectiveTo = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var isEffective = taxRate.IsEffective(DateTime.UtcNow);

        // Assert
        isEffective.Should().BeTrue();
    }

    [Fact]
    public void IsEffective_WhenNotActive_ShouldReturnFalse()
    {
        // Arrange
        var taxRate = new TaxRate
        {
            IsActive = false,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            EffectiveTo = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var isEffective = taxRate.IsEffective(DateTime.UtcNow);

        // Assert
        isEffective.Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenBeforeEffectiveFrom_ShouldReturnFalse()
    {
        // Arrange
        var taxRate = new TaxRate
        {
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var isEffective = taxRate.IsEffective(DateTime.UtcNow);

        // Assert
        isEffective.Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenAfterEffectiveTo_ShouldReturnFalse()
    {
        // Arrange
        var taxRate = new TaxRate
        {
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-60),
            EffectiveTo = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var isEffective = taxRate.IsEffective(DateTime.UtcNow);

        // Assert
        isEffective.Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenNoEffectiveTo_ShouldReturnTrue()
    {
        // Arrange
        var taxRate = new TaxRate
        {
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            EffectiveTo = null
        };

        // Act
        var isEffective = taxRate.IsEffective(DateTime.UtcNow);

        // Assert
        isEffective.Should().BeTrue();
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(50, true)]
    [InlineData(500, true)]
    public void AppliesToAmount_WithNoLimits_ShouldReturnTrue(decimal amount, bool expected)
    {
        // Arrange
        var taxRate = new TaxRate
        {
            MinimumTaxableAmount = null,
            MaximumTaxableAmount = null
        };

        // Act
        var applies = taxRate.AppliesToAmount(amount);

        // Assert
        applies.Should().Be(expected);
    }

    [Fact]
    public void AppliesToAmount_WithMinimum_ShouldValidateCorrectly()
    {
        // Arrange
        var taxRate = new TaxRate
        {
            MinimumTaxableAmount = 50m,
            MaximumTaxableAmount = null
        };

        // Act & Assert
        taxRate.AppliesToAmount(100m).Should().BeTrue();
        taxRate.AppliesToAmount(40m).Should().BeFalse();
    }

    [Fact]
    public void AppliesToAmount_WithMaximum_ShouldValidateCorrectly()
    {
        // Arrange
        var taxRate = new TaxRate
        {
            MinimumTaxableAmount = null,
            MaximumTaxableAmount = 200m
        };

        // Act & Assert
        taxRate.AppliesToAmount(100m).Should().BeTrue();
        taxRate.AppliesToAmount(300m).Should().BeFalse();
    }

    [Fact]
    public void AppliesToAmount_WithBothLimits_ShouldValidateCorrectly()
    {
        // Arrange
        var taxRate = new TaxRate
        {
            MinimumTaxableAmount = 50m,
            MaximumTaxableAmount = 200m
        };

        // Act & Assert
        taxRate.AppliesToAmount(100m).Should().BeTrue();
        taxRate.AppliesToAmount(40m).Should().BeFalse();
        taxRate.AppliesToAmount(300m).Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for RevenueEvent entity
/// </summary>
public class RevenueEventEntityTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var revenueEvent = new RevenueEvent();

        // Assert
        revenueEvent.Currency.Should().Be("USD");
        revenueEvent.ReferenceId.Should().BeEmpty();
        revenueEvent.Status.Should().Be(RevenueEventStatus.Pending);
        revenueEvent.ProcessedAt.Should().BeNull();
        revenueEvent.LedgerEntryId.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessed_ShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var revenueEvent = new RevenueEvent();
        var ledgerEntryId = Guid.NewGuid();

        // Act
        revenueEvent.MarkAsProcessed(ledgerEntryId);

        // Assert
        revenueEvent.Status.Should().Be(RevenueEventStatus.Processed);
        revenueEvent.ProcessedAt.Should().NotBeNull();
        revenueEvent.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        revenueEvent.LedgerEntryId.Should().Be(ledgerEntryId);
    }

    [Fact]
    public void MarkAsProcessed_WithoutLedgerEntry_ShouldUpdateStatusOnly()
    {
        // Arrange
        var revenueEvent = new RevenueEvent();

        // Act
        revenueEvent.MarkAsProcessed();

        // Assert
        revenueEvent.Status.Should().Be(RevenueEventStatus.Processed);
        revenueEvent.ProcessedAt.Should().NotBeNull();
        revenueEvent.LedgerEntryId.Should().BeNull();
    }

    [Theory]
    [InlineData(100.00)]
    [InlineData(999.99)]
    [InlineData(0.01)]
    public void Amount_ShouldAcceptValidAmounts(decimal amount)
    {
        // Arrange
        var revenueEvent = new RevenueEvent();

        // Act
        revenueEvent.Amount = amount;

        // Assert
        revenueEvent.Amount.Should().Be(amount);
    }

    [Theory]
    [InlineData(RevenueEventType.PaymentReceived)]
    [InlineData(RevenueEventType.RefundProcessed)]
    [InlineData(RevenueEventType.Chargeback)]
    public void EventType_ShouldAcceptAllTypes(RevenueEventType eventType)
    {
        // Arrange
        var revenueEvent = new RevenueEvent();

        // Act
        revenueEvent.EventType = eventType;

        // Assert
        revenueEvent.EventType.Should().Be(eventType);
    }

    [Theory]
    [InlineData(RevenueSource.Subscription)]
    [InlineData(RevenueSource.OneTimePayment)]
    [InlineData(RevenueSource.AddOn)]
    public void Source_ShouldAcceptAllSources(RevenueSource source)
    {
        // Arrange
        var revenueEvent = new RevenueEvent();

        // Act
        revenueEvent.Source = source;

        // Assert
        revenueEvent.Source.Should().Be(source);
    }
}
