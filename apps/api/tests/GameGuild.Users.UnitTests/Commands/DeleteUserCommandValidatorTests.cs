using FluentValidation.TestHelper;
using GameGuild.Users.Commands;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class DeleteUserCommandValidatorTests
{
    private readonly DeleteUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new DeleteUserCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteUserCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
