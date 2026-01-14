using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ResetUserNotificationPreferencesCommandValidatorTests
{
    private readonly ResetUserNotificationPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new ResetUserNotificationPreferencesCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var command = new ResetUserNotificationPreferencesCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
