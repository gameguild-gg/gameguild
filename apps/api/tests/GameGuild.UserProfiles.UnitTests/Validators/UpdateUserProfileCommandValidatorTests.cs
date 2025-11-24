using FluentValidation.TestHelper;
using GameGuild.UserProfiles;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Validators;

/// <summary>
/// Unit tests for UpdateUserProfileCommandValidator
/// </summary>
public class UpdateUserProfileCommandValidatorTests
{
    [Fact]
    public async Task Validate_ShouldFail_When_UserProfileId_IsEmpty()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var validator = new UpdateUserProfileCommandValidator(mockUserProfileRepository.Object);

        var command = new UpdateUserProfileCommand
        {
            UserProfileId = Guid.Empty,
            DisplayName = "Test User"
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserProfileId);
    }

    [Fact]
    public async Task Validate_ShouldFail_When_DisplayName_IsTooShort()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var userProfileId = Guid.NewGuid();

        mockUserProfileRepository.Setup(r => r.ExistsAsync(userProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var validator = new UpdateUserProfileCommandValidator(mockUserProfileRepository.Object);

        var command = new UpdateUserProfileCommand
        {
            UserProfileId = userProfileId,
            DisplayName = "A"
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public async Task Validate_ShouldPass_ForValidCommand()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var userProfileId = Guid.NewGuid();
        var displayName = "Valid User";

        mockUserProfileRepository.Setup(r => r.ExistsAsync(userProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockUserProfileRepository.Setup(r => r.IsDisplayNameUniqueAsync(displayName, userProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var validator = new UpdateUserProfileCommandValidator(mockUserProfileRepository.Object);

        var command = new UpdateUserProfileCommand
        {
            UserProfileId = userProfileId,
            DisplayName = displayName
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
