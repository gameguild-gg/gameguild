using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for SetSubscriptionAutoRenewCommandValidator
/// </summary>
public class SetSubscriptionAutoRenewCommandValidatorTests
{
    private readonly SetSubscriptionAutoRenewCommandValidator _validator;

    public SetSubscriptionAutoRenewCommandValidatorTests()
    {
        _validator = new SetSubscriptionAutoRenewCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand_AutoRenewTrue()
    {
        // Arrange
        var command = new SetSubscriptionAutoRenewCommand(
            SubscriptionId: Guid.NewGuid(),
            AutoRenew: true
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand_AutoRenewFalse()
    {
        // Arrange
        var command = new SetSubscriptionAutoRenewCommand(
            SubscriptionId: Guid.NewGuid(),
            AutoRenew: false
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSubscriptionIdIsEmpty()
    {
        // Arrange
        var command = new SetSubscriptionAutoRenewCommand(
            SubscriptionId: Guid.Empty,
            AutoRenew: true
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(SetSubscriptionAutoRenewCommand.SubscriptionId) &&
            e.ErrorMessage.Contains("required"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ShouldFail_WhenSubscriptionIdIsEmpty_RegardlessOfAutoRenew(bool autoRenew)
    {
        // Arrange
        var command = new SetSubscriptionAutoRenewCommand(
            SubscriptionId: Guid.Empty,
            AutoRenew: autoRenew
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }
}
