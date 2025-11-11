using FluentValidation.TestHelper;
using GameGuild.Users.Commands;
using GameGuild.Users.Models;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class UpdateUserPreferencesCommandValidatorTests
{
    private readonly UpdateUserPreferencesCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new UpdateUserPreferencesRequest(
            GeneralPreferences: new Dictionary<string, object?> { ["theme"] = "dark" }
        );
        var command = new UpdateUserPreferencesCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateUserPreferencesRequest(
            GeneralPreferences: new Dictionary<string, object?> { ["theme"] = "dark" }
        );
        var command = new UpdateUserPreferencesCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
