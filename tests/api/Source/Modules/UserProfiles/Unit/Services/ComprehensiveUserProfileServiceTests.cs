using GameGuild.Database;
using GameGuild.Modules.UserProfiles;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Tests.Modules.UserProfiles.Unit.Services
{
    /// <summary>
    /// Comprehensive test suite for UserProfileService
    /// Tests cover all service methods including CRUD operations, soft delete/restore, and edge cases
    /// </summary>
    public class ComprehensiveUserProfileServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserProfileService _service;

        public ComprehensiveUserProfileServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new UserProfileService(_context);
        }

        #region GetAllUserProfilesAsync Tests

        [Fact]
        public async Task GetAllUserProfilesAsync_WithActiveProfiles_ShouldReturnOnlyActiveProfiles()
        {
            // Arrange
            var activeProfiles = new List<UserProfile>
            {
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "John",
                    FamilyName = "Doe",
                    DisplayName = "johndoe",
                    Title = "Developer",
                    Description = "Software developer"
                },
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "Jane",
                    FamilyName = "Smith",
                    DisplayName = "janesmith",
                    Title = "Designer",
                    Description = "UI/UX designer"
                }
            };

            var deletedProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "Bob",
                FamilyName = "Johnson",
                DisplayName = "bobjohnson",
                Title = "Manager",
                Description = "Project manager"
            };
            deletedProfile.SoftDelete();

            _context.Resources.AddRange(activeProfiles);
            _context.Resources.Add(deletedProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllUserProfilesAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, profile => Assert.Null(profile.DeletedAt));
            Assert.Contains(result, p => p.GivenName == "John");
            Assert.Contains(result, p => p.GivenName == "Jane");
            Assert.DoesNotContain(result, p => p.GivenName == "Bob");
        }

        [Fact]
        public async Task GetAllUserProfilesAsync_WithNoProfiles_ShouldReturnEmptyCollection()
        {
            // Act
            var result = await _service.GetAllUserProfilesAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllUserProfilesAsync_WithOnlyDeletedProfiles_ShouldReturnEmptyCollection()
        {
            // Arrange
            var deletedProfiles = new List<UserProfile>
            {
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "Deleted1",
                    FamilyName = "User",
                    DisplayName = "deleted1",
                    Title = "",
                    Description = "Deleted user"
                },
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "Deleted2",
                    FamilyName = "User",
                    DisplayName = "deleted2",
                    Title = "",
                    Description = "Deleted user"
                }
            };

            deletedProfiles.ForEach(p => p.SoftDelete());
            _context.Resources.AddRange(deletedProfiles);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllUserProfilesAsync();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetUserProfileByIdAsync Tests

        [Fact]
        public async Task GetUserProfileByIdAsync_WithValidId_ShouldReturnProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetUserProfileByIdAsync(userProfile.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userProfile.Id, result.Id);
            Assert.Equal(userProfile.GivenName, result.GivenName);
            Assert.Equal(userProfile.FamilyName, result.FamilyName);
            Assert.Equal(userProfile.DisplayName, result.DisplayName);
        }

        [Fact]
        public async Task GetUserProfileByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetUserProfileByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserProfileByIdAsync_WithDeletedProfile_ShouldReturnNull()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };
            userProfile.SoftDelete();

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetUserProfileByIdAsync(userProfile.Id);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetUserProfileByUserIdAsync Tests

        [Fact]
        public async Task GetUserProfileByUserIdAsync_WithValidUserId_ShouldReturnProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userProfile = new UserProfile
            {
                Id = userId, // UserProfile ID matches User ID for 1:1 relationship
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetUserProfileByUserIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal(userProfile.GivenName, result.GivenName);
            Assert.Equal(userProfile.FamilyName, result.FamilyName);
        }

        [Fact]
        public async Task GetUserProfileByUserIdAsync_WithNonExistentUserId_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetUserProfileByUserIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserProfileByUserIdAsync_WithDeletedProfile_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userProfile = new UserProfile
            {
                Id = userId,
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };
            userProfile.SoftDelete();

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetUserProfileByUserIdAsync(userId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateUserProfileAsync Tests

        [Fact]
        public async Task CreateUserProfileAsync_WithValidProfile_ShouldCreateAndReturnProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            // Act
            var result = await _service.CreateUserProfileAsync(userProfile);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userProfile.Id, result.Id);
            Assert.Equal(userProfile.GivenName, result.GivenName);
            Assert.Equal(userProfile.FamilyName, result.FamilyName);

            // Verify it was saved to database
            var savedProfile = await _context.Resources.OfType<UserProfile>().FirstOrDefaultAsync(p => p.Id == userProfile.Id);
            Assert.NotNull(savedProfile);
            Assert.Equal(userProfile.GivenName, savedProfile.GivenName);
        }

        [Fact]
        public async Task CreateUserProfileAsync_WithMinimalData_ShouldCreateProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "J",
                FamilyName = "D",
                DisplayName = "jd",
                Title = "",
                Description = null
            };

            // Act
            var result = await _service.CreateUserProfileAsync(userProfile);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("J", result.GivenName);
            Assert.Equal("D", result.FamilyName);
            Assert.Equal("jd", result.DisplayName);
        }

        [Fact]
        public async Task CreateUserProfileAsync_WithMaximalData_ShouldCreateProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = new string('A', 100), // Max length
                FamilyName = new string('B', 100),
                DisplayName = new string('C', 100),
                Title = new string('D', 200),
                Description = new string('E', 1000)
            };

            // Act
            var result = await _service.CreateUserProfileAsync(userProfile);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.GivenName?.Length);
            Assert.Equal(100, result.FamilyName?.Length);
            Assert.Equal(100, result.DisplayName?.Length);
            Assert.Equal(200, result.Title?.Length);
            Assert.Equal(1000, result.Description?.Length);
        }

        #endregion

        #region UpdateUserProfileAsync Tests

        [Fact]
        public async Task UpdateUserProfileAsync_WithValidIdAndData_ShouldUpdateProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            var updatedProfile = new UserProfile
            {
                Id = userProfile.Id,
                GivenName = "John Updated",
                FamilyName = "Doe Updated",
                DisplayName = "johndoe_updated",
                Title = "Senior Developer",
                Description = "Senior software developer"
            };

            // Act
            var result = await _service.UpdateUserProfileAsync(userProfile.Id, updatedProfile);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John Updated", result.GivenName);
            Assert.Equal("Doe Updated", result.FamilyName);
            Assert.Equal("johndoe_updated", result.DisplayName);
            Assert.Equal("Senior Developer", result.Title);
            Assert.Equal("Senior software developer", result.Description);

            // Verify changes were saved
            var dbProfile = await _context.Resources.OfType<UserProfile>().FirstOrDefaultAsync(p => p.Id == userProfile.Id);
            Assert.Equal("John Updated", dbProfile?.GivenName);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var updatedProfile = new UserProfile
            {
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            // Act
            var result = await _service.UpdateUserProfileAsync(Guid.NewGuid(), updatedProfile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_WithDeletedProfile_ShouldReturnNull()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };
            userProfile.SoftDelete();

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            var updatedProfile = new UserProfile
            {
                GivenName = "John Updated",
                FamilyName = "Doe Updated",
                DisplayName = "johndoe_updated",
                Title = "Senior Developer",
                Description = "Senior software developer"
            };

            // Act
            var result = await _service.UpdateUserProfileAsync(userProfile.Id, updatedProfile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_WithPartialData_ShouldUpdateOnlyProvidedFields()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            var partialUpdate = new UserProfile
            {
                GivenName = "John Updated",
                FamilyName = userProfile.FamilyName, // Keep original
                DisplayName = userProfile.DisplayName, // Keep original
                Title = "Senior Developer", // Update
                Description = userProfile.Description // Keep original
            };

            // Act
            var result = await _service.UpdateUserProfileAsync(userProfile.Id, partialUpdate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John Updated", result.GivenName);
            Assert.Equal("Doe", result.FamilyName); // Original value
            Assert.Equal("johndoe", result.DisplayName); // Original value
            Assert.Equal("Senior Developer", result.Title);
            Assert.Equal("Software developer", result.Description); // Original value
        }

        #endregion

        #region DeleteUserProfileAsync Tests

        [Fact]
        public async Task DeleteUserProfileAsync_WithValidId_ShouldHardDeleteProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteUserProfileAsync(userProfile.Id);

            // Assert
            Assert.True(result);

            // Verify profile is completely removed from database
            var deletedProfile = await _context.Resources.OfType<UserProfile>().FirstOrDefaultAsync(p => p.Id == userProfile.Id);
            Assert.Null(deletedProfile);
        }

        [Fact]
        public async Task DeleteUserProfileAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DeleteUserProfileAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUserProfileAsync_WithAlreadyDeletedProfile_ShouldStillDelete()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };
            userProfile.SoftDelete();

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteUserProfileAsync(userProfile.Id);

            // Assert
            Assert.True(result); // Hard delete should work even on soft-deleted profiles

            // Verify profile is completely removed
            var deletedProfile = await _context.Resources.OfType<UserProfile>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == userProfile.Id);
            Assert.Null(deletedProfile);
        }

        #endregion

        #region SoftDeleteUserProfileAsync Tests

        [Fact]
        public async Task SoftDeleteUserProfileAsync_WithValidId_ShouldSoftDeleteProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.SoftDeleteUserProfileAsync(userProfile.Id);

            // Assert
            Assert.True(result);

            // Verify profile is soft deleted
            var softDeletedProfile = await _context.Resources.OfType<UserProfile>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == userProfile.Id);
            Assert.NotNull(softDeletedProfile);
            Assert.NotNull(softDeletedProfile.DeletedAt);

            // Verify profile is not accessible through normal queries
            var accessibleProfile = await _context.Resources.OfType<UserProfile>()
                .Where(p => p.DeletedAt == null)
                .FirstOrDefaultAsync(p => p.Id == userProfile.Id);
            Assert.Null(accessibleProfile);
        }

        [Fact]
        public async Task SoftDeleteUserProfileAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Act
            var result = await _service.SoftDeleteUserProfileAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SoftDeleteUserProfileAsync_WithAlreadySoftDeletedProfile_ShouldReturnFalse()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };
            userProfile.SoftDelete();

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.SoftDeleteUserProfileAsync(userProfile.Id);

            // Assert
            Assert.False(result); // Should return false as it's already soft deleted
        }

        #endregion

        #region RestoreUserProfileAsync Tests

        [Fact]
        public async Task RestoreUserProfileAsync_WithSoftDeletedProfile_ShouldRestoreProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };
            userProfile.SoftDelete();

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RestoreUserProfileAsync(userProfile.Id);

            // Assert
            Assert.True(result);

            // Verify profile is restored
            var restoredProfile = await _context.Resources.OfType<UserProfile>().FirstOrDefaultAsync(p => p.Id == userProfile.Id);
            Assert.NotNull(restoredProfile);
            Assert.Null(restoredProfile.DeletedAt);
            Assert.Equal("John", restoredProfile.GivenName);
        }

        [Fact]
        public async Task RestoreUserProfileAsync_WithActiveProfile_ShouldReturnFalse()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.RestoreUserProfileAsync(userProfile.Id);

            // Assert
            Assert.False(result); // Should return false as profile is not deleted
        }

        [Fact]
        public async Task RestoreUserProfileAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Act
            var result = await _service.RestoreUserProfileAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetDeletedUserProfilesAsync Tests

        [Fact]
        public async Task GetDeletedUserProfilesAsync_WithDeletedProfiles_ShouldReturnOnlyDeletedProfiles()
        {
            // Arrange
            var activeProfiles = new List<UserProfile>
            {
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "Active1",
                    FamilyName = "User",
                    DisplayName = "active1",
                    Title = "",
                    Description = "Active user"
                },
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "Active2",
                    FamilyName = "User",
                    DisplayName = "active2",
                    Title = "",
                    Description = "Active user"
                }
            };

            var deletedProfiles = new List<UserProfile>
            {
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "Deleted1",
                    FamilyName = "User",
                    DisplayName = "deleted1",
                    Title = "",
                    Description = "Deleted user"
                },
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "Deleted2",
                    FamilyName = "User",
                    DisplayName = "deleted2",
                    Title = "",
                    Description = "Deleted user"
                }
            };

            deletedProfiles.ForEach(p => p.SoftDelete());

            _context.Resources.AddRange(activeProfiles);
            _context.Resources.AddRange(deletedProfiles);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDeletedUserProfilesAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, profile => Assert.NotNull(profile.DeletedAt));
            Assert.Contains(result, p => p.GivenName == "Deleted1");
            Assert.Contains(result, p => p.GivenName == "Deleted2");
            Assert.DoesNotContain(result, p => p.GivenName == "Active1");
            Assert.DoesNotContain(result, p => p.GivenName == "Active2");
        }

        [Fact]
        public async Task GetDeletedUserProfilesAsync_WithNoDeletedProfiles_ShouldReturnEmptyCollection()
        {
            // Arrange
            var activeProfiles = new List<UserProfile>
            {
                new UserProfile
                {
                    Id = Guid.NewGuid(),
                    GivenName = "Active1",
                    FamilyName = "User",
                    DisplayName = "active1",
                    Title = "",
                    Description = "Active user"
                }
            };

            _context.Resources.AddRange(activeProfiles);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDeletedUserProfilesAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDeletedUserProfilesAsync_WithNoProfiles_ShouldReturnEmptyCollection()
        {
            // Act
            var result = await _service.GetDeletedUserProfilesAsync();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task UserProfileLifecycle_CreateUpdateSoftDeleteRestore_ShouldWorkCorrectly()
        {
            // Arrange & Act - Create
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            var created = await _service.CreateUserProfileAsync(userProfile);
            Assert.NotNull(created);

            // Act - Update
            var updatedProfile = new UserProfile
            {
                GivenName = "John Updated",
                FamilyName = "Doe Updated",
                DisplayName = "johndoe_updated",
                Title = "Senior Developer",
                Description = "Senior software developer"
            };

            var updated = await _service.UpdateUserProfileAsync(created.Id, updatedProfile);
            Assert.NotNull(updated);
            Assert.Equal("John Updated", updated.GivenName);

            // Act - Soft Delete
            var softDeleted = await _service.SoftDeleteUserProfileAsync(created.Id);
            Assert.True(softDeleted);

            // Verify profile is not accessible
            var inaccessible = await _service.GetUserProfileByIdAsync(created.Id);
            Assert.Null(inaccessible);

            // Act - Restore
            var restored = await _service.RestoreUserProfileAsync(created.Id);
            Assert.True(restored);

            // Verify profile is accessible again
            var accessible = await _service.GetUserProfileByIdAsync(created.Id);
            Assert.NotNull(accessible);
            Assert.Equal("John Updated", accessible.GivenName);

            // Act - Hard Delete
            var hardDeleted = await _service.DeleteUserProfileAsync(created.Id);
            Assert.True(hardDeleted);

            // Verify profile is completely gone
            var gone = await _context.Resources.OfType<UserProfile>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == created.Id);
            Assert.Null(gone);
        }

        [Fact]
        public async Task ConcurrentOperations_MultipleUpdates_ShouldHandleCorrectly()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Developer",
                Description = "Software developer"
            };

            await _service.CreateUserProfileAsync(userProfile);

            // Act - Simulate concurrent updates
            var update1 = new UserProfile
            {
                GivenName = "John1",
                FamilyName = userProfile.FamilyName,
                DisplayName = userProfile.DisplayName,
                Title = userProfile.Title,
                Description = userProfile.Description
            };

            var update2 = new UserProfile
            {
                GivenName = "John2",
                FamilyName = userProfile.FamilyName,
                DisplayName = userProfile.DisplayName,
                Title = userProfile.Title,
                Description = userProfile.Description
            };

            var result1 = await _service.UpdateUserProfileAsync(userProfile.Id, update1);
            var result2 = await _service.UpdateUserProfileAsync(userProfile.Id, update2);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            // Last update wins
            Assert.Equal("John2", result2.GivenName);
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
