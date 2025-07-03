using GameGuild.Modules.Users.Services;
using Moq;
using Xunit;
using UserModel = GameGuild.Modules.Users.Models.User;


namespace GameGuild.Tests.Modules.Users.Unit.Services {
  public class UserServiceTests {
    private readonly Mock<IUserService> _mockService;

    public UserServiceTests() { _mockService = new Mock<IUserService>(); }

    [Fact]
    public async Task Should_Create_User_Successfully() {
      // Arrange
      var newUser = new UserModel { Name = "Test User", Email = "test@example.com" };

      _mockService.Setup(s => s.CreateUserAsync(It.IsAny<UserModel>()))
                  .ReturnsAsync((UserModel u) => {
                      u.Id = Guid.NewGuid();
                      u.CreatedAt = DateTime.UtcNow;
                      u.UpdatedAt = DateTime.UtcNow;

                      return u;
                    }
                  );

      // Act
      var result = await _mockService.Object.CreateUserAsync(newUser);

      // Assert
      Assert.NotNull(result);
      Assert.NotEqual(Guid.Empty, result.Id);
      Assert.Equal("Test User", result.Name);
      Assert.Equal("test@example.com", result.Email);
      Assert.NotEqual(default, result.CreatedAt);

      // Verify service was called correctly
      _mockService.Verify(s => s.CreateUserAsync(It.IsAny<UserModel>()), Times.Once);
    }

    [Fact]
    public async Task Should_Throw_Exception_When_Creating_User_With_Duplicate_Email() {
      // Arrange
      _mockService.Setup(s => s.CreateUserAsync(It.IsAny<UserModel>()))
                  .ThrowsAsync(new InvalidOperationException("A user with email 'test@example.com' already exists."));

      var newUser = new UserModel { Name = "Test User", Email = "test@example.com" };

      // Act & Assert
      await Assert.ThrowsAsync<InvalidOperationException>(() =>
                                                            _mockService.Object.CreateUserAsync(newUser)
      );

      // Verify service was called correctly
      _mockService.Verify(s => s.CreateUserAsync(It.IsAny<UserModel>()), Times.Once);
    }

    [Fact]
    public async Task Should_Get_User_By_Id() {
      // Arrange
      var userId = Guid.NewGuid();
      var user = new UserModel { Id = userId, Name = "Test User", Email = "test@example.com" };

      _mockService.Setup(s => s.GetUserByIdAsync(userId))
                  .ReturnsAsync(user);

      // Act
      var result = await _mockService.Object.GetUserByIdAsync(userId);

      // Assert
      Assert.NotNull(result);
      Assert.Equal(userId, result.Id);
      Assert.Equal("Test User", result.Name);

      // Verify service was called
      _mockService.Verify(s => s.GetUserByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Should_Update_User() {
      // Arrange
      var userId = Guid.NewGuid();
      var existingUser = new UserModel { Id = userId, Name = "Test User", Email = "test@example.com" };

      var updatedUser = new UserModel { Id = userId, Name = "Updated Name", Email = "test@example.com" };

      _mockService.Setup(s => s.GetUserByIdAsync(userId))
                  .ReturnsAsync(existingUser);

      _mockService.Setup(s => s.UpdateUserAsync(userId, It.IsAny<UserModel>()))
                  .ReturnsAsync((Guid id, UserModel u) => {
                      u.UpdatedAt = DateTime.UtcNow;

                      return u;
                    }
                  );

      // Act
      var result = await _mockService.Object.UpdateUserAsync(userId, updatedUser);

      // Assert
      Assert.NotNull(result);
      Assert.Equal("Updated Name", result.Name);
      Assert.Equal("test@example.com", result.Email);

      // Verify service calls
      _mockService.Verify(s => s.UpdateUserAsync(userId, It.IsAny<UserModel>()), Times.Once);
    }

    [Fact]
    public async Task Should_Delete_User() {
      // Arrange
      var userId = Guid.NewGuid();

      _mockService.Setup(s => s.DeleteUserAsync(userId))
                  .ReturnsAsync(true);

      // Act
      var result = await _mockService.Object.DeleteUserAsync(userId);

      // Assert
      Assert.True(result);

      // Verify service call
      _mockService.Verify(s => s.DeleteUserAsync(userId), Times.Once);
    }
  }
}
