using FluentValidation.TestHelper;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ReplaceUserAccessibilityPreferencesCommandValidatorTests
{
    private readonly ReplaceUserAccessibilityPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var prefs = new Dictionary<string, object?> { { "HighContrast", true } };
        var request = new ReplaceUserAccessibilityPreferencesRequest(prefs);
        var command = new ReplaceUserAccessibilityPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var prefs = new Dictionary<string, object?> { { "HighContrast", true } };
        var request = new ReplaceUserAccessibilityPreferencesRequest(prefs);
        var command = new ReplaceUserAccessibilityPreferencesCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
