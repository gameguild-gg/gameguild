using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Validators;

/// <summary>
/// Unit tests for UpdateUserCommandValidator
/// </summary>
public class UpdateUserCommandValidatorTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly UpdateUserCommandValidator _validator;

    public UpdateUserCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new UpdateUserCommandValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = new UpdateUserCommand { UserId = Guid.Empty };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenUserIdIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new UpdateUserCommand { UserId = userId };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
