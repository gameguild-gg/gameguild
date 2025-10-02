using FluentValidation.TestHelper;
using GameGuild.Modules.UserProfiles;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Validators;

/// <summary>
/// Unit tests for DeleteUserProfileCommandValidator
/// </summary>
public class DeleteUserProfileCommandValidatorTests
{
    [Fact]
    public async Task Validate_ShouldFail_When_UserProfileId_IsEmpty()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var validator = new DeleteUserProfileCommandValidator(mockUserProfileRepository.Object);

        var command = new DeleteUserProfileCommand
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

        mockUserProfileRepository.Setup(r => r.ExistsAsync(userProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var validator = new DeleteUserProfileCommandValidator(mockUserProfileRepository.Object);

        var command = new DeleteUserProfileCommand
        {
            UserProfileId = userProfileId
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
