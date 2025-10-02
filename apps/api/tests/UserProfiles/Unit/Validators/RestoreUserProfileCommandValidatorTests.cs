using FluentValidation.TestHelper;
using GameGuild.Modules.UserProfiles;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Validators;

/// <summary>
/// Unit tests for RestoreUserProfileCommandValidator
/// </summary>
public class RestoreUserProfileCommandValidatorTests
{
    [Fact]
    public async Task Validate_ShouldFail_When_UserProfileId_IsEmpty()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var validator = new RestoreUserProfileCommandValidator(mockUserProfileRepository.Object);

        var command = new RestoreUserProfileCommand
        {
            UserProfileId = Guid.Empty
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserProfileId);
    }

    [Fact]
    public async Task Validate_ShouldPass_ForValidCommand()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var userProfileId = Guid.NewGuid();

        mockUserProfileRepository.Setup(r => r.DeletedExistsAsync(userProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var validator = new RestoreUserProfileCommandValidator(mockUserProfileRepository.Object);

        var command = new RestoreUserProfileCommand
        {
            UserProfileId = userProfileId
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
