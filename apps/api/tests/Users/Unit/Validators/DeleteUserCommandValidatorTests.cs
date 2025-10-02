using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Validators;

/// <summary>
/// Unit tests for DeleteUserCommandValidator
/// </summary>
public class DeleteUserCommandValidatorTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly DeleteUserCommandValidator _validator;

    public DeleteUserCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new DeleteUserCommandValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = new DeleteUserCommand { UserId = Guid.Empty };

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

        var command = new DeleteUserCommand { UserId = userId };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
