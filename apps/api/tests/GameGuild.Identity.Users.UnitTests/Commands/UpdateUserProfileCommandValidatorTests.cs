using FluentValidation.TestHelper;
using GameGuild.Identity.Users;
using GameGuild.Social.Profiles;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UpdateUserProfileCommandValidatorTests
{
    private readonly UpdateUserProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(DisplayName: "Test User");
        var command = new UpdateUserProfileCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(DisplayName: "Test User");
        var command = new UpdateUserProfileCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WithNullRequest_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(Guid.NewGuid(), null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Request);
    }
}
