using FluentValidation.TestHelper;
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
            DisplayName: "DisplayName",
            Bio: "Bio",
            Location: "Location",
            Website: "https://website.com",
            JobTitle: "Engineer",
            Company: "GameGuild",
            TimeZone: "UTC",
            Language: "en-US",
            ProfileVisibility: "Public",
            ShowEmail: true,
            ShowLocation: true
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
            DisplayName: "DisplayName",
            Bio: "Bio",
            Location: "Location",
            Website: "https://website.com",
            JobTitle: "Engineer",
            Company: "GameGuild",
            TimeZone: "UTC",
            Language: "en-US",
            ProfileVisibility: "Public",
            ShowEmail: true,
            ShowLocation: true
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
            DisplayName: "DisplayName",
            Bio: "Bio",
            Location: "Location",
            Website: "https://website.com",
            JobTitle: "Engineer",
            Company: "GameGuild",
            TimeZone: "UTC",
            Language: "en-US",
            ProfileVisibility: "",
            ShowEmail: true,
            ShowLocation: true
        );
        var command = new ReplaceUserProfileCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Request.ProfileVisibility");
    }
}
