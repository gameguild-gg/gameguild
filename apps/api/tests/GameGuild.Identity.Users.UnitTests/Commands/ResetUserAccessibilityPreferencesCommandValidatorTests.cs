using FluentValidation.TestHelper;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ResetUserAccessibilityPreferencesCommandValidatorTests
{
    private readonly ResetUserAccessibilityPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new ResetUserAccessibilityPreferencesCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var command = new ResetUserAccessibilityPreferencesCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
