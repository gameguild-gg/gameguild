using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for UpdateSubscriptionMetadataCommandValidator
/// </summary>
public class UpdateSubscriptionMetadataCommandValidatorTests
{
    private readonly UpdateSubscriptionMetadataCommandValidator _validator;

    public UpdateSubscriptionMetadataCommandValidatorTests()
    {
        _validator = new UpdateSubscriptionMetadataCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new UpdateSubscriptionMetadataCommand(
            SubscriptionId: Guid.NewGuid(),
            Metadata: "{\"key\": \"value\"}"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WithMaxLengthMetadata()
    {
        // Arrange
        var command = new UpdateSubscriptionMetadataCommand(
            SubscriptionId: Guid.NewGuid(),
            Metadata: new string('a', 2000)
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
        var command = new UpdateSubscriptionMetadataCommand(
            SubscriptionId: Guid.Empty,
            Metadata: "{\"key\": \"value\"}"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateSubscriptionMetadataCommand.SubscriptionId) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenMetadataIsEmpty()
    {
        // Arrange
        var command = new UpdateSubscriptionMetadataCommand(
            SubscriptionId: Guid.NewGuid(),
            Metadata: ""
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateSubscriptionMetadataCommand.Metadata) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenMetadataIsNull()
    {
        // Arrange
        var command = new UpdateSubscriptionMetadataCommand(
            SubscriptionId: Guid.NewGuid(),
            Metadata: null!
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateSubscriptionMetadataCommand.Metadata));
    }

    [Fact]
    public void Validate_ShouldFail_WhenMetadataExceeds2000Characters()
    {
        // Arrange
        var command = new UpdateSubscriptionMetadataCommand(
            SubscriptionId: Guid.NewGuid(),
            Metadata: new string('a', 2001)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateSubscriptionMetadataCommand.Metadata) &&
            e.ErrorMessage.Contains("2000 characters"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenBothFieldsInvalid()
    {
        // Arrange
        var command = new UpdateSubscriptionMetadataCommand(
            SubscriptionId: Guid.Empty,
            Metadata: ""
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("{\"simple\": \"json\"}")]
    [InlineData("{\"nested\": {\"key\": \"value\"}}")]
    [InlineData("{\"array\": [1, 2, 3]}")]
    [InlineData("plain text metadata")]
    public void Validate_ShouldPass_WithVariousMetadataFormats(string metadata)
    {
        // Arrange
        var command = new UpdateSubscriptionMetadataCommand(
            SubscriptionId: Guid.NewGuid(),
            Metadata: metadata
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
