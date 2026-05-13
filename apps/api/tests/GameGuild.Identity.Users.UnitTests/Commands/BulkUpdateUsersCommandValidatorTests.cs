using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkUpdateUsersCommandValidatorTests
{
    private readonly BulkUpdateUsersCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUpdates_ShouldNotHaveAnyValidationErrors()
    {
        var updates = new List<UpdateUserRequestItem>
        {
            new(Guid.NewGuid(), "User One", null),
            new(Guid.NewGuid(), "User Two", "+1234567890")
        };
        var command = new BulkUpdateUsersCommand(updates);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUpdates_ShouldHaveError()
    {
        var command = new BulkUpdateUsersCommand(Array.Empty<UpdateUserRequestItem>());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Updates);
    }

    [Fact]
    public void Validate_WithInvalidUpdateItem_ShouldHaveNestedValidationError()
    {
        var updates = new List<UpdateUserRequestItem>
        {
            new(Guid.Empty, "", null)
        };
        var command = new BulkUpdateUsersCommand(updates);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Updates[0].UserId");
        result.ShouldHaveValidationErrorFor("Updates[0].Name");
    }
}
