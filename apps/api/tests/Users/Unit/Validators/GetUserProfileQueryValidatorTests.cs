using FluentValidation.TestHelper;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Validators;

/// <summary>
/// Unit tests for GetUserProfileQueryValidator
/// </summary>
public class GetUserProfileQueryValidatorTests
{
    private readonly GetUserProfileQueryValidator _validator;

    public GetUserProfileQueryValidatorTests()
    {
        _validator = new GetUserProfileQueryValidator();
    }

    [Fact]
    public async Task Validate_ShouldPass_ForValidQuery()
    {
        // Arrange
        var query = new GetUserProfileQuery { UserId = Guid.NewGuid() };

        // Act & Assert
        var result = await _validator.TestValidateAsync(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
