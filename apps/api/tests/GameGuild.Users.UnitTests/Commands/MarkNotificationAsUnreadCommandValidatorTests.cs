using FluentValidation.TestHelper;
using GameGuild.Users.Commands;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class MarkNotificationAsUnreadCommandValidatorTests
{
    private readonly MarkNotificationAsUnreadCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new MarkNotificationAsUnreadCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var command = new MarkNotificationAsUnreadCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WithEmptyNotificationId_ShouldHaveError()
    {
        // Arrange
        var command = new MarkNotificationAsUnreadCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NotificationId);
    }
}
