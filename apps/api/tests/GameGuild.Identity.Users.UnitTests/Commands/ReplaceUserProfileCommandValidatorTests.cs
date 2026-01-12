using FluentValidation.TestHelper;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ReplaceUserProfileCommandValidatorTests
{
    private readonly ReplaceUserProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var request = new ReplaceUserProfileRequest(
            "DisplayName",
            "Bio",
            "Location",
            "https://website.com",
            "UTC",
            "en-US",
            "Public",
            true,
            true
        );
        var command = new ReplaceUserProfileCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var request = new ReplaceUserProfileRequest(
            "DisplayName",
            "Bio",
            "Location",
            "https://website.com",
            "UTC",
            "en-US",
            "Public",
            true,
            true
        );
        var command = new ReplaceUserProfileCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WithEmptyProfileVisibility_ShouldHaveError()
    {
        // Arrange
        var request = new ReplaceUserProfileRequest(
            "DisplayName",
            "Bio",
            "Location",
            "https://website.com",
            "UTC",
            "en-US",
            "",  // Empty ProfileVisibility
            true,
            true
        );
        var command = new ReplaceUserProfileCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Request.ProfileVisibility");
    }
}
