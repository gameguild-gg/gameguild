using FluentValidation.TestHelper;
using GameGuild.Users.Commands;
using GameGuild.Users.Models;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class UpdateUserNotificationPreferencesCommandValidatorTests
{
    private readonly UpdateUserNotificationPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var notificationPrefs = new Dictionary<string, object?>
        {
            { "EmailEnabled", true },
            { "PushEnabled", false }
        };
        var request = new UpdateUserNotificationPreferencesRequest(notificationPrefs);
        var command = new UpdateUserNotificationPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var notificationPrefs = new Dictionary<string, object?>
        {
            { "EmailEnabled", true }
        };
        var request = new UpdateUserNotificationPreferencesRequest(notificationPrefs);
        var command = new UpdateUserNotificationPreferencesCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WithEmptyNotificationPreferences_ShouldHaveError()
    {
        // Arrange
        var emptyPrefs = new Dictionary<string, object?>();
        var request = new UpdateUserNotificationPreferencesRequest(emptyPrefs);
        var command = new UpdateUserNotificationPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Request.NotificationPreferences");
    }
}
