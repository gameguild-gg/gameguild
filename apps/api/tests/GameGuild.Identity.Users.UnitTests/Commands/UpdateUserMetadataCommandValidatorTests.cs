using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UpdateUserMetadataCommandValidatorTests
{
    private readonly UpdateUserMetadataCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var request = new UpdateUserMetadataRequest(
            CustomFields: new Dictionary<string, object?> { ["test"] = "value" }
        );
        var command = new UpdateUserMetadataCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var request = new UpdateUserMetadataRequest(
            CustomFields: new Dictionary<string, object?> { ["test"] = "value" }
        );
        var command = new UpdateUserMetadataCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
