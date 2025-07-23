using Microsoft.EntityFrameworkCore;
using GameGuild.Database;
using GameGuild.Modules.Users;


namespace GameGuild.Tests.Modules.Users.Unit.Services
{
    /// <summary>
    /// Comprehensive test suite for UserService
    /// Tests all service layer operations and business logic for actual existing methods
    /// </summary>
    public class ComprehensiveUserServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;

        public ComprehensiveUserServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _userService = new UserService(_context);
        }

        #region GetUserById Tests

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var user = new User
            {
                Name = "Test User",
                Email = "test@example.com",
                IsActive = true,
                Balance = 100m,
                AvailableBalance = 100m,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserByIdAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("Test User", result.Name);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _userService.GetUserByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetUserByEmail Tests

        [Fact]
        public async Task GetByEmailAsync_WithValidEmail_ShouldReturnUser()
        {
            // Arrange
            var user = new User
            {
                Name = "Email Test User",
                Email = "email.test@example.com",
                IsActive = true,
                Balance = 50m,
                AvailableBalance = 50m,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetByEmailAsync("email.test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("email.test@example.com", result.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_WithNonExistentEmail_ShouldReturnNull()
        {
            // Act
            var result = await _userService.GetByEmailAsync("nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByEmailAsync_WithCaseInsensitiveEmail_ShouldReturnUser()
        {
            // Arrange
            var user = new User
            {
                Name = "Case Test User",
                Email = "Case.Test@Example.Com",
                IsActive = true,
                Balance = 25m,
                AvailableBalance = 25m,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetByEmailAsync("Case.Test@Example.Com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }

        #endregion

        #region GetAllUsers Tests

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new[]
            {
                new User { Name = "User 1", Email = "user1@example.com", IsActive = true, Balance = 0, AvailableBalance = 0 },
                new User { Name = "User 2", Email = "user2@example.com", IsActive = true, Balance = 0, AvailableBalance = 0 },
                new User { Name = "User 3", Email = "user3@example.com", IsActive = false, Balance = 0, AvailableBalance = 0 },
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetAllUsersAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region CreateUser Tests

        [Fact]
        public async Task CreateUserAsync_WithValidUser_ShouldCreateAndReturnUser()
        {
            // Arrange
            var user = new User
            {
                Name = "New User",
                Email = "new@example.com",
                IsActive = true,
                Balance = 150m,
                AvailableBalance = 150m,
            };

            // Act
            var result = await _userService.CreateUserAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New User", result.Name);
            Assert.Equal("new@example.com", result.Email);
            Assert.Equal(150m, result.Balance);
            Assert.Equal(150m, result.AvailableBalance);
            Assert.True(result.IsActive);

            // Verify in database
            var savedUser = await _context.Users.FindAsync(result.Id);
            Assert.NotNull(savedUser);
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var existingUser = new User
            {
                Name = "Existing User",
                Email = "duplicate@example.com",
                IsActive = true,
                Balance = 0,
                AvailableBalance = 0,
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var newUser = new User
            {
                Name = "New User",
                Email = "duplicate@example.com",
                IsActive = true,
                Balance = 100m,
                AvailableBalance = 100m,
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.CreateUserAsync(newUser));
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateUserAsync_WithValidUser_ShouldUpdateUser()
        {
            // Arrange
            var existingUser = new User
            {
                Name = "Original Name",
                Email = "original@example.com",
                IsActive = true,
                Balance = 100m,
                AvailableBalance = 100m,
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var updatedUser = new User
            {
                Name = "Updated Name",
                Email = "updated@example.com",
                IsActive = true,
                Balance = 100m,
                AvailableBalance = 100m,
            };

            // Act
            var result = await _userService.UpdateUserAsync(existingUser.Id, updatedUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("updated@example.com", result.Email);
            Assert.Equal(existingUser.Id, result.Id);
        }

        [Fact]
        public async Task UpdateUserAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var updatedUser = new User
            {
                Name = "New Name",
                Email = "new@example.com",
                IsActive = true,
                Balance = 0,
                AvailableBalance = 0,
            };

            // Act
            var result = await _userService.UpdateUserAsync(Guid.NewGuid(), updatedUser);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteUser Tests (Hard Delete)

        [Fact]
        public async Task DeleteUserAsync_WithValidId_ShouldDeleteUser()
        {
            // Arrange
            var user = new User
            {
                Name = "User To Delete",
                Email = "delete@example.com",
                IsActive = true,
                Balance = 50m,
                AvailableBalance = 50m,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.DeleteUserAsync(user.Id);

            // Assert
            Assert.True(result);

            // Verify user is completely removed
            var deletedUser = await _context.Users.FindAsync(user.Id);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task DeleteUserAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Act
            var result = await _userService.DeleteUserAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        #endregion

        #region SoftDeleteUser Tests

        [Fact]
        public async Task SoftDeleteUserAsync_WithValidId_ShouldSoftDeleteUser()
        {
            // Arrange
            var user = new User
            {
                Name = "User To Soft Delete",
                Email = "softdelete@example.com",
                IsActive = true,
                Balance = 50m,
                AvailableBalance = 50m,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.SoftDeleteUserAsync(user.Id);

            // Assert
            Assert.True(result);

            // Verify user is soft deleted (still exists but marked as deleted)
            var softDeletedUser = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.NotNull(softDeletedUser);
            Assert.NotNull(softDeletedUser.DeletedAt);
        }

        [Fact]
        public async Task SoftDeleteUserAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Act
            var result = await _userService.SoftDeleteUserAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        #endregion

        #region RestoreUser Tests

        [Fact]
        public async Task RestoreUserAsync_WithSoftDeletedUser_ShouldRestoreUser()
        {
            // Arrange
            var user = new User
            {
                Name = "User To Restore",
                Email = "restore@example.com",
                IsActive = true,
                Balance = 50m,
                AvailableBalance = 50m,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Soft delete first
            await _userService.SoftDeleteUserAsync(user.Id);

            // Act
            var result = await _userService.RestoreUserAsync(user.Id);

            // Assert
            Assert.True(result);

            // Verify user is restored
            var restoredUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(restoredUser);
            Assert.Null(restoredUser.DeletedAt);
        }

        [Fact]
        public async Task RestoreUserAsync_WithActiveUser_ShouldReturnFalse()
        {
            // Arrange
            var user = new User
            {
                Name = "Active User",
                Email = "active@example.com",
                IsActive = true,
                Balance = 50m,
                AvailableBalance = 50m,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act - Try to restore a user that isn't deleted
            var result = await _userService.RestoreUserAsync(user.Id);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RestoreUserAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Act
            var result = await _userService.RestoreUserAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetDeletedUsers Tests

        [Fact]
        public async Task GetDeletedUsersAsync_ShouldReturnOnlyDeletedUsers()
        {
            // Arrange
            var activeUsers = new[]
            {
                new User { Name = "Active 1", Email = "active1@example.com", IsActive = true, Balance = 0, AvailableBalance = 0 },
                new User { Name = "Active 2", Email = "active2@example.com", IsActive = true, Balance = 0, AvailableBalance = 0 },
            };

            var userToDelete = new User
            {
                Name = "To Delete",
                Email = "todelete@example.com",
                IsActive = true,
                Balance = 0,
                AvailableBalance = 0,
            };

            _context.Users.AddRange(activeUsers);
            _context.Users.Add(userToDelete);
            await _context.SaveChangesAsync();

            // Soft delete one user
            await _userService.SoftDeleteUserAsync(userToDelete.Id);

            // Act
            var result = await _userService.GetDeletedUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(result, u => u.Email == "todelete@example.com");
            Assert.All(result, u => Assert.NotNull(u.DeletedAt));
        }

        [Fact]
        public async Task GetDeletedUsersAsync_WithNoDeletedUsers_ShouldReturnEmptyList()
        {
            // Arrange
            var activeUsers = new[]
            {
                new User { Name = "Active 1", Email = "active1@example.com", IsActive = true, Balance = 0, AvailableBalance = 0 },
                new User { Name = "Active 2", Email = "active2@example.com", IsActive = true, Balance = 0, AvailableBalance = 0 },
            };

            _context.Users.AddRange(activeUsers);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetDeletedUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task UserLifecycle_CreateUpdateSoftDeleteRestore_ShouldWorkCorrectly()
        {
            // Arrange & Act - Create
            var user = new User
            {
                Name = "Lifecycle User",
                Email = "lifecycle@example.com",
                IsActive = true,
                Balance = 100m,
                AvailableBalance = 100m,
            };

            var createdUser = await _userService.CreateUserAsync(user);
            Assert.NotNull(createdUser);

            // Act - Update
            var updatedUserData = new User
            {
                Name = "Updated Lifecycle User",
                Email = "updated.lifecycle@example.com",
                IsActive = true,
                Balance = 100m,
                AvailableBalance = 100m,
            };

            var updatedUser = await _userService.UpdateUserAsync(createdUser.Id, updatedUserData);
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated Lifecycle User", updatedUser.Name);

            // Act - Soft Delete
            var softDeleted = await _userService.SoftDeleteUserAsync(createdUser.Id);
            Assert.True(softDeleted);

            // Verify user is not accessible through normal queries
            var inaccessible = await _userService.GetUserByIdAsync(createdUser.Id);
            Assert.Null(inaccessible);

            // Act - Restore
            var restored = await _userService.RestoreUserAsync(createdUser.Id);
            Assert.True(restored);

            // Verify user is accessible again
            var accessible = await _userService.GetUserByIdAsync(createdUser.Id);
            Assert.NotNull(accessible);
            Assert.Equal("Updated Lifecycle User", accessible.Name);

            // Act - Hard Delete
            var hardDeleted = await _userService.DeleteUserAsync(createdUser.Id);
            Assert.True(hardDeleted);

            // Verify user is completely gone
            var gone = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == createdUser.Id);
            Assert.Null(gone);
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
