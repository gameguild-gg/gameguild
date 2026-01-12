using FluentValidation.TestHelper;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkActivateUsersCommandValidatorTests
{
    private readonly BulkActivateUsersCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new BulkActivateUsersCommand(userIds);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserIds_ShouldHaveError()
    {
        // Arrange
        var command = new BulkActivateUsersCommand(new List<Guid>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }
}
