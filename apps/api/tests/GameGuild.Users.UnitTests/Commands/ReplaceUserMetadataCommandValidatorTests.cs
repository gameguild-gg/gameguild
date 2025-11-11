using FluentValidation.TestHelper;
using GameGuild.Users.Commands;
using GameGuild.Users.Models;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class ReplaceUserMetadataCommandValidatorTests
{
    private readonly ReplaceUserMetadataCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var customFields = new Dictionary<string, object?> { { "field1", "value1" } };
        var tags = new List<string> { "tag1" };
        var externalRefs = new Dictionary<string, string> { { "system1", "ref1" } };
        var request = new ReplaceUserMetadataRequest(customFields, tags, externalRefs);
        var command = new ReplaceUserMetadataCommand(Guid.NewGuid(), request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var customFields = new Dictionary<string, object?> { { "field1", "value1" } };
        var tags = new List<string> { "tag1" };
        var externalRefs = new Dictionary<string, string> { { "system1", "ref1" } };
        var request = new ReplaceUserMetadataRequest(customFields, tags, externalRefs);
        var command = new ReplaceUserMetadataCommand(Guid.Empty, request);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
