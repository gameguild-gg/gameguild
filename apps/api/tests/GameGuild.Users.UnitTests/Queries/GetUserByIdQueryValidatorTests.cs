using FluentValidation.TestHelper;
using GameGuild.Users.Queries;
using Xunit;

namespace GameGuild.Users.UnitTests.Queries;

public class GetUserByIdQueryValidatorTests
{
    private readonly GetUserByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var query = new GetUserByIdQuery(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var query = new GetUserByIdQuery(Guid.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
