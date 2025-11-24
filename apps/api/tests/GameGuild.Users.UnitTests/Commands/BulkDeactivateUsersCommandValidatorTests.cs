using FluentValidation.TestHelper;
using GameGuild.Users.Commands;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class BulkDeactivateUsersCommandValidatorTests
{
    private readonly BulkDeactivateUsersCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserIds_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new BulkDeactivateUsersCommand(userIds);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserIdsList_ShouldHaveError()
    {
        // Arrange
        var command = new BulkDeactivateUsersCommand(new List<Guid>());

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
        var command = new BulkDeactivateUsersCommand(userIds);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }
}
