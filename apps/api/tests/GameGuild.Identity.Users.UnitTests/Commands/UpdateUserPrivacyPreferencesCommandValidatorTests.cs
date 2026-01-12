using FluentValidation.TestHelper;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UpdateUserPrivacyPreferencesCommandValidatorTests
{
    private readonly UpdateUserPrivacyPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var privacyPrefs = new Dictionary<string, object?>
        {
            { "ProfileVisible", true },
            { "ShowActivity", false }
        };
        var request = new UpdateUserPrivacyPreferencesRequest(privacyPrefs);
        var command = new UpdateUserPrivacyPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var privacyPrefs = new Dictionary<string, object?>
        {
            { "ProfileVisible", true }
        };
        var request = new UpdateUserPrivacyPreferencesRequest(privacyPrefs);
        var command = new UpdateUserPrivacyPreferencesCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WithEmptyPrivacyPreferences_ShouldHaveError()
    {
        // Arrange
        var emptyPrefs = new Dictionary<string, object?>();
        var request = new UpdateUserPrivacyPreferencesRequest(emptyPrefs);
        var command = new UpdateUserPrivacyPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Request.PrivacyPreferences");
    }
}
