using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkSuspendUsersCommandValidatorTests
{
    private readonly BulkSuspendUsersCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserIds_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new BulkSuspendUsersCommand(userIds);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserIdsList_ShouldHaveError()
    {
        // Arrange
        var command = new BulkSuspendUsersCommand(new List<Guid>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }

    [Fact]
    public void Validate_WithEmptyGuidInList_ShouldHaveError()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.Empty };
        var command = new BulkSuspendUsersCommand(userIds);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }
}
