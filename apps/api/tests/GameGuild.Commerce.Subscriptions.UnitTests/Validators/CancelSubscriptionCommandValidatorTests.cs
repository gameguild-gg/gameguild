using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

public class CancelSubscriptionCommandValidatorTests
{
    private readonly CancelSubscriptionCommandValidator _validator;

    public CancelSubscriptionCommandValidatorTests()
    {
        _validator = new CancelSubscriptionCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new CancelSubscriptionCommand(
            SubscriptionId: Guid.NewGuid(),
            Reason: CancellationReason.UserRequested,
            Note: "Requested via support ticket",
            EffectiveDate: DateTime.UtcNow.AddDays(7)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSubscriptionIdIsEmpty()
    {
        // Arrange
        var command = new CancelSubscriptionCommand(
            SubscriptionId: Guid.Empty,
            Reason: CancellationReason.UserRequested,
            Note: null,
            EffectiveDate: DateTime.UtcNow
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CancelSubscriptionCommand.SubscriptionId));
    }

    [Fact]
    public void Validate_ShouldPass_WithNullNote()
    {
        // Arrange
        var command = new CancelSubscriptionCommand(
            SubscriptionId: Guid.NewGuid(),
            Reason: CancellationReason.UserRequested,
            Note: null,
            EffectiveDate: DateTime.UtcNow
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
