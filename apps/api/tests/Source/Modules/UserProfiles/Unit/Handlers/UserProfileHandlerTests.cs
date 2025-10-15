using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;


namespace GameGuild.Tests.Modules.UserProfiles.Unit.Handlers {
  public class UserProfileHandlerTests : IDisposable {
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<CreateUserProfileHandler>> _createLoggerMock;
    private readonly Mock<ILogger<UpdateUserProfileHandler>> _updateLoggerMock;
    private readonly Mock<ILogger<DeleteUserProfileHandler>> _deleteLoggerMock;
    private readonly Mock<IDomainEventPublisher> _eventPublisherMock;
    private readonly CreateUserProfileHandler _createHandler;
    private readonly UpdateUserProfileHandler _updateHandler;
    private readonly DeleteUserProfileHandler _deleteHandler;

    public UserProfileHandlerTests() {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

      _context = new ApplicationDbContext(options);
      _createLoggerMock = new Mock<ILogger<CreateUserProfileHandler>>();
      _updateLoggerMock = new Mock<ILogger<UpdateUserProfileHandler>>();
      _deleteLoggerMock = new Mock<ILogger<DeleteUserProfileHandler>>();
      _eventPublisherMock = new Mock<IDomainEventPublisher>();

      _createHandler = new CreateUserProfileHandler(_context, _createLoggerMock.Object, _eventPublisherMock.Object);
      _updateHandler = new UpdateUserProfileHandler(_context, _updateLoggerMock.Object, _eventPublisherMock.Object);
      _deleteHandler = new DeleteUserProfileHandler(_context, _deleteLoggerMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Should_Handle_UserProfile_Creation_Command() {
      // Arrange
      var userId = Guid.NewGuid();
      var command = new CreateUserProfileCommand {
        UserId = userId,
        GivenName = "Test",
        FamilyName = "User",
        DisplayName = "Test User",
        Title = "Developer",
        Description = "Test description",
      };

      // Act
      var result = await _createHandler.Handle(command, CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.NotNull(result.Value);
      Assert.Equal(userId, result.Value.Id);
      Assert.Equal("Test", result.Value.GivenName);
      Assert.Equal("User", result.Value.FamilyName);
      Assert.Equal("Test User", result.Value.DisplayName);
      Assert.Equal("Developer", result.Value.Title);
      Assert.Equal("Test description", result.Value.Description);
    }

    [Fact]
    public async Task Should_Handle_UserProfile_Update_Command() {
      // Arrange
      var userId = Guid.NewGuid();

      // First create a profile
      var createCommand = new CreateUserProfileCommand {
        UserId = userId,
        GivenName = "Original",
        FamilyName = "User",
        DisplayName = "Original User",
        Title = "Developer",
        Description = "Original description",
      };

      await _createHandler.Handle(createCommand, CancellationToken.None);

      // Then update it
      var updateCommand = new UpdateUserProfileCommand {
        UserProfileId = userId,
        GivenName = "Updated",
        FamilyName = "User",
        DisplayName = "Updated User",
        Title = "Senior Developer",
        Description = "Updated description",
      };

      // Act
      var result = await _updateHandler.Handle(updateCommand, CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.NotNull(result.Value);
      Assert.Equal(userId, result.Value.Id);
      Assert.Equal("Updated", result.Value.GivenName);
      Assert.Equal("Updated User", result.Value.DisplayName);
      Assert.Equal("Senior Developer", result.Value.Title);
    }

    [Fact]
    public async Task Should_Handle_UserProfile_Delete_Command() {
      // Arrange
      var userId = Guid.NewGuid();

      // First create a profile
      var createCommand = new CreateUserProfileCommand {
        UserId = userId,
        GivenName = "Test",
        FamilyName = "User",
        DisplayName = "Test User",
        Title = "Developer",
        Description = "Test description",
      };

      await _createHandler.Handle(createCommand, CancellationToken.None);

      // Then delete it
      var deleteCommand = new DeleteUserProfileCommand {
        UserProfileId = userId,
        SoftDelete = true,
      };

      // Act
      var result = await _deleteHandler.Handle(deleteCommand, CancellationToken.None);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.True(result.Value);

      // Verify the profile is soft deleted
      var deletedProfile = await _context.Resources.OfType<UserProfile>()
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(up => up.Id == userId);

      Assert.NotNull(deletedProfile);
      Assert.NotNull(deletedProfile.DeletedAt);
    }

    public void Dispose() {
      _context.Dispose();
    }
  }
}
