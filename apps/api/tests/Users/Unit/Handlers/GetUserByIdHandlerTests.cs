using FluentAssertions;
using GameGuild.Database;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Handlers;

/// <summary>
/// Unit tests for GetUserByIdHandler
/// </summary>
public class GetUserByIdHandlerTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly GetUserByIdHandler _handler;

    public GetUserByIdHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _handler = new GetUserByIdHandler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_Should_Return_User_When_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User { Id = userId, Email = "test@test.com" };
        _context.Users.Add(expectedUser);
        await _context.SaveChangesAsync();

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_User_Not_Found()
    {
        // Arrange
        var query = new GetUserByIdQuery { UserId = Guid.NewGuid() };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Exclude_Deleted_Users_By_Default()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deletedUser = new User { Id = userId, Email = "deleted@test.com", DeletedAt = DateTime.UtcNow };
        _context.Users.Add(deletedUser);
        await _context.SaveChangesAsync();

        var query = new GetUserByIdQuery { UserId = userId, IncludeDeleted = false };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Include_Deleted_Users_When_Requested()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deletedUser = new User { Id = userId, Email = "deleted@test.com", DeletedAt = DateTime.UtcNow };
        _context.Users.Add(deletedUser);
        await _context.SaveChangesAsync();

        var query = new GetUserByIdQuery { UserId = userId, IncludeDeleted = true };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
    }
}
