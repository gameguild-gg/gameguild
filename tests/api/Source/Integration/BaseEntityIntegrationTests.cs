using GameGuild.Data;
using GameGuild.Modules.Users.Services;
using Microsoft.EntityFrameworkCore;
using UserModel = GameGuild.Modules.Users.Models.User;


namespace GameGuild.API.Tests.Integration;

public class BaseEntityIntegrationTests {
  private static ApplicationDbContext GetInMemoryContext() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase(Guid.NewGuid().ToString())
                  .Options;

    return new ApplicationDbContext(options);
  }

  [Fact]
  public async Task User_BaseEntityProperties_ShouldWork() {
    // Arrange
    using var context = GetInMemoryContext();
    var userService = new UserService(context);

    // Act - Create user using BaseEntity constructor pattern
    var user = new UserModel(new { Name = "Test User", Email = "test@example.com" });
    var createdUser = await userService.CreateUserAsync(user);

    // Assert
    Assert.NotEqual(Guid.Empty, createdUser.Id);
    Assert.Equal("Test User", createdUser.Name);
    Assert.Equal("test@example.com", createdUser.Email);
    Assert.True(createdUser.CreatedAt > DateTime.MinValue);
    Assert.True(createdUser.UpdatedAt > DateTime.MinValue);
    Assert.Null(createdUser.DeletedAt);
    Assert.False(createdUser.IsDeleted);

    // Version is set to 1 when entity is saved to the database by ApplicationDbContext.UpdateTimestamps()
    Assert.Equal(1, createdUser.Version);
  }

  [Fact]
  public async Task User_SoftDelete_ShouldWork() {
    // Arrange
    using var context = GetInMemoryContext();
    var userService = new UserService(context);

    var user = new UserModel(new { Name = "Delete Test", Email = "delete@example.com" });
    var createdUser = await userService.CreateUserAsync(user);

    // Act
    await userService.SoftDeleteUserAsync(createdUser.Id);

    // Assert
    var activeUsers = await userService.GetAllUsersAsync();
    var deletedUsers = await userService.GetDeletedUsersAsync();

    Assert.DoesNotContain(activeUsers, u => u.Id == createdUser.Id);
    Assert.Contains(deletedUsers, u => u.Id == createdUser.Id);

    var deletedUser = deletedUsers.First(u => u.Id == createdUser.Id);
    Assert.True(deletedUser.IsDeleted);
    Assert.NotNull(deletedUser.DeletedAt);
  }

  [Fact]
  public async Task User_RestoreAfterSoftDelete_ShouldWork() {
    // Arrange
    using var context = GetInMemoryContext();
    var userService = new UserService(context);

    var user = new UserModel(new { Name = "Restore Test", Email = "restore@example.com" });
    var createdUser = await userService.CreateUserAsync(user);

    // Act
    await userService.SoftDeleteUserAsync(createdUser.Id);
    await userService.RestoreUserAsync(createdUser.Id);

    // Assert
    var activeUsers = await userService.GetAllUsersAsync();
    var deletedUsers = await userService.GetDeletedUsersAsync();

    Assert.Contains(activeUsers, u => u.Id == createdUser.Id);
    Assert.DoesNotContain(deletedUsers, u => u.Id == createdUser.Id);

    var restoredUser = activeUsers.First(u => u.Id == createdUser.Id);
    Assert.False(restoredUser.IsDeleted);
    Assert.Null(restoredUser.DeletedAt);
  }
}
