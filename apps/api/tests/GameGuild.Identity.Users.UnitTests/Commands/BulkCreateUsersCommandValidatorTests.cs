using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkCreateUsersCommandValidatorTests
{
    private readonly BulkCreateUsersCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var users = new List<CreateUserRequestItem>
        {
            new("user1@example.com", "User One", null),
            new("user2@example.com", "User Two", "1234567890")
        };
        var command = new BulkCreateUsersCommand(users);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUsersList_ShouldHaveError()
    {
        // Arrange
        var users = new List<CreateUserRequestItem>();
        var command = new BulkCreateUsersCommand(users);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users);
    }
}
