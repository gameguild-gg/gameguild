using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UpdateUserAccessibilityPreferencesCommandValidatorTests
{
    private readonly UpdateUserAccessibilityPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var accessibilityPrefs = JsonMap(new Dictionary<string, object?>
        {
            { "HighContrast", true },
            { "ScreenReaderEnabled", false }
        });
        var request = new UpdateUserAccessibilityPreferencesRequest(accessibilityPrefs);
        var command = new UpdateUserAccessibilityPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var accessibilityPrefs = JsonMap(new Dictionary<string, object?>
        {
            { "HighContrast", true }
        });
        var request = new UpdateUserAccessibilityPreferencesRequest(accessibilityPrefs);
        var command = new UpdateUserAccessibilityPreferencesCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WithEmptyAccessibilityPreferences_ShouldHaveError()
    {
        // Arrange
        var emptyPrefs = JsonMap(new Dictionary<string, object?>());
        var request = new UpdateUserAccessibilityPreferencesRequest(emptyPrefs);
        var command = new UpdateUserAccessibilityPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Request.AccessibilityPreferences");
    }
}
