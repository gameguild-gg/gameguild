using FluentValidation.TestHelper;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ReplaceUserPrivacyPreferencesCommandValidatorTests
{
    private readonly ReplaceUserPrivacyPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var prefs = new Dictionary<string, object?> { { "ProfileVisible", true } };
        var request = new ReplaceUserPrivacyPreferencesRequest(prefs);
        var command = new ReplaceUserPrivacyPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var prefs = new Dictionary<string, object?> { { "ProfileVisible", true } };
        var request = new ReplaceUserPrivacyPreferencesRequest(prefs);
        var command = new ReplaceUserPrivacyPreferencesCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
