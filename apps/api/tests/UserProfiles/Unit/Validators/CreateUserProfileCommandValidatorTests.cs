using FluentValidation.TestHelper;
using GameGuild.Modules.UserProfiles;
using GameGuild.Modules.Users;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Validators;

/// <summary>
/// Unit tests for CreateUserProfileCommandValidator
/// </summary>
public class CreateUserProfileCommandValidatorTests
{
    [Fact]
    public async Task Validate_ShouldFail_When_DisplayName_IsEmpty()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var mockUserRepository = new Mock<IUserRepository>();
        var validator = new CreateUserProfileCommandValidator(mockUserProfileRepository.Object, mockUserRepository.Object);

        var command = new CreateUserProfileCommand
        {
            DisplayName = "",
            UserId = Guid.NewGuid()
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public async Task Validate_ShouldFail_When_DisplayName_IsTooShort()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var mockUserRepository = new Mock<IUserRepository>();
        var validator = new CreateUserProfileCommandValidator(mockUserProfileRepository.Object, mockUserRepository.Object);

        var command = new CreateUserProfileCommand
        {
            DisplayName = "A",
            UserId = Guid.NewGuid()
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public async Task Validate_ShouldFail_When_UserId_IsEmpty()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var mockUserRepository = new Mock<IUserRepository>();
        var validator = new CreateUserProfileCommandValidator(mockUserProfileRepository.Object, mockUserRepository.Object);

        var command = new CreateUserProfileCommand
        {
            DisplayName = "Test User",
            UserId = Guid.Empty
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_ShouldPass_ForValidCommand()
    {
        // Arrange
        var mockUserProfileRepository = new Mock<IUserProfileRepository>();
        var mockUserRepository = new Mock<IUserRepository>();

        var userId = Guid.NewGuid();
        var displayName = "Valid User";

        mockUserProfileRepository.Setup(r => r.IsDisplayNameUniqueAsync(displayName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });

        mockUserProfileRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var validator = new CreateUserProfileCommandValidator(mockUserProfileRepository.Object, mockUserRepository.Object);

        var command = new CreateUserProfileCommand
        {
            DisplayName = displayName,
            UserId = userId
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
