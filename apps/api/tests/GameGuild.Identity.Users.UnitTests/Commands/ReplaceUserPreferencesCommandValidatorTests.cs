using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ReplaceUserPreferencesCommandValidatorTests
{
    private readonly ReplaceUserPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var generalPrefs = JsonMap(new Dictionary<string, object?> { { "theme", "dark" } });
        var notificationPrefs = JsonMap(new Dictionary<string, object?> { { "email", true } });
        var accessibilityPrefs = JsonMap(new Dictionary<string, object?> { { "highContrast", false } });
        var privacyPrefs = JsonMap(new Dictionary<string, object?> { { "profileVisible", true } });
        var request = new ReplaceUserPreferencesRequest(
            generalPrefs,
            notificationPrefs,
            accessibilityPrefs,
            privacyPrefs
        );
        var command = new ReplaceUserPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var generalPrefs = JsonMap(new Dictionary<string, object?> { { "theme", "dark" } });
        var notificationPrefs = JsonMap(new Dictionary<string, object?> { { "email", true } });
        var accessibilityPrefs = JsonMap(new Dictionary<string, object?> { { "highContrast", false } });
        var privacyPrefs = JsonMap(new Dictionary<string, object?> { { "profileVisible", true } });
        var request = new ReplaceUserPreferencesRequest(
            generalPrefs,
            notificationPrefs,
            accessibilityPrefs,
            privacyPrefs
        );
        var command = new ReplaceUserPreferencesCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
