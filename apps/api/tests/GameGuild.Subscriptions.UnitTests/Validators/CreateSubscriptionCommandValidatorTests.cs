using GameGuild.ValueObjects;
using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using Xunit;

namespace GameGuild.Subscriptions.UnitTests.Validators;

public class CreateSubscriptionCommandValidatorTests
{
    private readonly CreateSubscriptionCommandValidator _validator;

    public CreateSubscriptionCommandValidatorTests()
    {
        _validator = new CreateSubscriptionCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new CreateSubscriptionCommand(
            TenantId: Guid.NewGuid(),
            PlanId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            BillingCycle: BillingCycle.Monthly,
            Amount: 29.99m,
            StartDate: DateTime.UtcNow.AddDays(1),
            TrialDays: 14
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTenantIdIsEmpty()
    {
        // Arrange
        var command = new CreateSubscriptionCommand(
            TenantId: Guid.Empty,
            PlanId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            BillingCycle: BillingCycle.Monthly,
            Amount: 29.99m,
            StartDate: null,
            TrialDays: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSubscriptionCommand.TenantId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenAmountIsNegative()
    {
        // Arrange
        var command = new CreateSubscriptionCommand(
            TenantId: Guid.NewGuid(),
            PlanId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            BillingCycle: BillingCycle.Monthly,
            Amount: -10m,
            StartDate: null,
            TrialDays: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSubscriptionCommand.Amount));
    }

    [Fact]
    public void Validate_ShouldPass_WhenAmountIsZero()
    {
        // Arrange
        var command = new CreateSubscriptionCommand(
            TenantId: Guid.NewGuid(),
            PlanId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            BillingCycle: BillingCycle.Monthly,
            Amount: 0m,
            StartDate: null,
            TrialDays: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
