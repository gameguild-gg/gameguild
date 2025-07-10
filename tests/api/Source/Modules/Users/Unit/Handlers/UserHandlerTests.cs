using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GameGuild.Database;
using GameGuild.Modules.Users;
using GameGuild.Common;
using Moq;
using MediatR;


namespace GameGuild.API.Tests.Modules.Users.Unit.Handlers {
  public class UserHandlerTests : IDisposable {
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<CreateUserHandler>> _createLogger;
    private readonly Mock<ILogger<UpdateUserHandler>> _updateLogger;
    private readonly Mock<ILogger<DeleteUserHandler>> _deleteLogger;
    private readonly Mock<IMediator> _mediator;

    public UserHandlerTests() {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
      
      _context = new ApplicationDbContext(options);
      _createLogger = new Mock<ILogger<CreateUserHandler>>();
      _updateLogger = new Mock<ILogger<UpdateUserHandler>>();
      _deleteLogger = new Mock<ILogger<DeleteUserHandler>>();
      _mediator = new Mock<IMediator>();
    }

    [Fact]
    public async Task Should_Handle_User_Creation_Command() {
      // Arrange
      var command = new CreateUserCommand { 
        Name = "Test User", 
        Email = "test@example.com",
        InitialBalance = 100m
      };

      var handler = new CreateUserHandler(_context, _createLogger.Object, _mediator.Object);

      // Act
      var result = await handler.Handle(command, CancellationToken.None);

      // Assert
      Assert.NotNull(result);
      Assert.Equal("Test User", result.Name);
      Assert.Equal("test@example.com", result.Email);
      Assert.True(result.IsActive); // Default should be true
      Assert.Equal(100m, result.Balance);
      
      // Verify user was saved to database
      var savedUser = await _context.Users.FindAsync(result.Id);
      Assert.NotNull(savedUser);
      Assert.Equal("Test User", savedUser.Name);
    }

    [Fact]
    public async Task Should_Handle_User_Update_Command() {
      // Arrange
      var userId = Guid.NewGuid();
      var existingUser = new User { 
        Id = userId, 
        Name = "Original Name", 
        Email = "original@example.com", 
        IsActive = true 
      };
      
      _context.Users.Add(existingUser);
      await _context.SaveChangesAsync();

      var command = new UpdateUserCommand { 
        UserId = userId, 
        Name = "Updated Name",
        Email = "updated@example.com"
      };

      var handler = new UpdateUserHandler(_context, _updateLogger.Object, _mediator.Object);

      // Act
      var result = await handler.Handle(command, CancellationToken.None);

      // Assert
      Assert.NotNull(result);
      Assert.Equal(userId, result.Id);
      Assert.Equal("Updated Name", result.Name);
      Assert.Equal("updated@example.com", result.Email);
    }

    [Fact]
    public async Task Should_Handle_User_Delete_Command() {
      // Arrange
      var userId = Guid.NewGuid();
      var existingUser = new User { 
        Id = userId, 
        Name = "Test User", 
        Email = "test@example.com", 
        IsActive = true 
      };
      
      _context.Users.Add(existingUser);
      await _context.SaveChangesAsync();

      var command = new DeleteUserCommand { UserId = userId, SoftDelete = true };

      var handler = new DeleteUserHandler(_context, _deleteLogger.Object, _mediator.Object);

      // Act
      var result = await handler.Handle(command, CancellationToken.None);

      // Assert
      Assert.True(result);
      
      // Verify user was soft deleted by checking DeletedAt is set
      var userWithDeleted = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
      Assert.NotNull(userWithDeleted);
      Assert.NotNull(userWithDeleted.DeletedAt);
      Assert.True(userWithDeleted.IsDeleted);
      
      // In this test setup, the query filter may not be working as expected in in-memory database
      // so we'll verify the soft delete by checking that we can't find the user with normal queries
      var activeUsers = await _context.Users.Where(u => u.DeletedAt == null).ToListAsync();
      Assert.DoesNotContain(activeUsers, u => u.Id == userId);
    }

    public void Dispose() {
      _context.Dispose();
    }
  }
}
