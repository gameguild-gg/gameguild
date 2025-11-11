using FluentValidation.TestHelper;
using GameGuild.Users.Commands;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class DeactivateUserCommandValidatorTests
{
    private readonly DeactivateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new DeactivateUserCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var command = new DeactivateUserCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
