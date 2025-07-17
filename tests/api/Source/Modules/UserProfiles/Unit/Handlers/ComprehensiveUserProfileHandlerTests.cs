using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;


namespace GameGuild.Tests.Modules.UserProfiles.Unit.Handlers
{
    /// <summary>
    /// Comprehensive test suite for all UserProfile CQRS handlers
    /// Tests cover Create, Update, Delete, Query operations with edge cases and validation
    /// </summary>
    public class ComprehensiveUserProfileHandlerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CreateUserProfileHandler>> _createLogger;
        private readonly Mock<ILogger<UpdateUserProfileHandler>> _updateLogger;
        private readonly Mock<ILogger<DeleteUserProfileHandler>> _deleteLogger;
    private readonly Mock<ILogger<GetUserProfileByIdHandler>> _getByIdLogger;
    private readonly Mock<ILogger<GetUserProfileByUserIdHandler>> _getByUserIdLogger;
    private readonly Mock<ILogger<GetAllUserProfilesHandler>> _getAllLogger;
    private readonly Mock<ILogger<SearchUserProfilesHandler>> _searchLogger;
    private readonly Mock<ILogger<GetUserProfileStatisticsHandler>> _statisticsLogger;
    private readonly Mock<ILogger<BulkDeleteUserProfilesHandler>> _bulkDeleteLogger;
    private readonly Mock<ILogger<BulkRestoreUserProfilesHandler>> _bulkRestoreLogger;
        private readonly Mock<IDomainEventPublisher> _eventPublisher;

        public ComprehensiveUserProfileHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _createLogger = new Mock<ILogger<CreateUserProfileHandler>>();
            _updateLogger = new Mock<ILogger<UpdateUserProfileHandler>>();        _deleteLogger = new Mock<ILogger<DeleteUserProfileHandler>>();
        _getByIdLogger = new Mock<ILogger<GetUserProfileByIdHandler>>();
        _getByUserIdLogger = new Mock<ILogger<GetUserProfileByUserIdHandler>>();
        _getAllLogger = new Mock<ILogger<GetAllUserProfilesHandler>>();
        _searchLogger = new Mock<ILogger<SearchUserProfilesHandler>>();
        _statisticsLogger = new Mock<ILogger<GetUserProfileStatisticsHandler>>();
        _bulkDeleteLogger = new Mock<ILogger<BulkDeleteUserProfilesHandler>>();
        _bulkRestoreLogger = new Mock<ILogger<BulkRestoreUserProfilesHandler>>();
        _eventPublisher = new Mock<IDomainEventPublisher>();
        }

        #region Test Data Setup

        private async Task<User> CreateTestUserAsync()
        {
            var user = new User
            {
                Name = "Test User",
                Email = $"test.user.{Guid.NewGuid()}@example.com",
                IsActive = true,
                Balance = 100m,
                AvailableBalance = 100m
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        #endregion

        #region Create UserProfile Tests

        [Fact]
        public async Task CreateUserProfile_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var handler = new CreateUserProfileHandler(_context, _createLogger.Object, _eventPublisher.Object);
            var command = new CreateUserProfileCommand
            {
                UserId = user.Id,
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe",
                Title = "Software Developer",
                Description = "Passionate developer"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(command.GivenName, result.Value.GivenName);
            Assert.Equal(command.FamilyName, result.Value.FamilyName);
            Assert.Equal(command.DisplayName, result.Value.DisplayName);
            Assert.Equal(command.Title, result.Value.Title);
            Assert.Equal(command.Description, result.Value.Description);
            Assert.Equal(command.UserId, result.Value.Id); // UserProfile ID matches User ID
        }

        [Fact]
        public async Task CreateUserProfile_WithExistingProfile_ShouldReturnError()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var existingProfile = new UserProfile
            {
                Id = user.Id, // UserProfile ID is the same as User ID
                GivenName = "Jane",
                FamilyName = "Smith",
                DisplayName = "janesmith",
                Title = "",
                Description = "Existing user"
            };
            
            _context.Resources.Add(existingProfile);
            await _context.SaveChangesAsync();

            var handler = new CreateUserProfileHandler(_context, _createLogger.Object, _eventPublisher.Object);
            var command = new CreateUserProfileCommand
            {
                UserId = user.Id,
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("already exists", result.Error.Description);
        }

        #endregion

        #region Update UserProfile Tests

        [Fact]
        public async Task UpdateUserProfile_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var userProfile = new UserProfile
            {
                Id = user.Id,
                GivenName = "Jane",
                FamilyName = "Smith",
                DisplayName = "janesmith",
                Title = "Designer",
                Description = "Creative designer"
            };
            
            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            var handler = new UpdateUserProfileHandler(_context, _updateLogger.Object, _eventPublisher.Object);
            var command = new UpdateUserProfileCommand
            {
                UserProfileId = userProfile.Id,
                GivenName = "Jane Updated",
                FamilyName = "Smith Updated",
                DisplayName = "janeupdated",
                Title = "Senior Designer",
                Description = "Senior creative designer"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(command.GivenName, result.Value.GivenName);
            Assert.Equal(command.FamilyName, result.Value.FamilyName);
            Assert.Equal(command.DisplayName, result.Value.DisplayName);
            Assert.Equal(command.Title, result.Value.Title);
            Assert.Equal(command.Description, result.Value.Description);
        }

        [Fact]
        public async Task UpdateUserProfile_WithNonExistentId_ShouldReturnError()
        {
            // Arrange
            var handler = new UpdateUserProfileHandler(_context, _updateLogger.Object, _eventPublisher.Object);
            var command = new UpdateUserProfileCommand
            {
                UserProfileId = Guid.NewGuid(),
                GivenName = "John",
                FamilyName = "Doe",
                DisplayName = "johndoe"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error.Description);
        }

        #endregion

        #region Delete UserProfile Tests

        [Fact]
        public async Task DeleteUserProfile_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var userProfile = new UserProfile
            {
                Id = user.Id,
                GivenName = "Jane",
                FamilyName = "Smith",
                DisplayName = "janesmith",
                Title = "",
                Description = "Test user"
            };
            
            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            var handler = new DeleteUserProfileHandler(_context, _deleteLogger.Object, _eventPublisher.Object);
            var command = new DeleteUserProfileCommand
            {
                UserProfileId = userProfile.Id
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Verify soft delete
            var deletedProfile = await _context.Resources.OfType<UserProfile>()
                .FirstOrDefaultAsync(up => up.Id == userProfile.Id);
            Assert.NotNull(deletedProfile);
            Assert.NotNull(deletedProfile.DeletedAt);
        }

        [Fact]
        public async Task DeleteUserProfile_WithNonExistentId_ShouldReturnError()
        {
            // Arrange
            var handler = new DeleteUserProfileHandler(_context, _deleteLogger.Object, _eventPublisher.Object);
            var command = new DeleteUserProfileCommand
            {
                UserProfileId = Guid.NewGuid()
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error.Description);
        }

        #endregion

        #region Query UserProfile Tests

        [Fact]
        public async Task GetUserProfileById_WithValidId_ShouldReturnProfile()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var userProfile = new UserProfile
            {
                Id = user.Id,
                GivenName = "Query",
                FamilyName = "User",
                DisplayName = "queryuser",
                Title = "Tester",
                Description = "Test profile"
            };
            
            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            var handler = new GetUserProfileByIdHandler(_context, _getByIdLogger.Object);
            var query = new GetUserProfileByIdQuery
            {
                UserProfileId = userProfile.Id
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(userProfile.GivenName, result.Value.GivenName);
            Assert.Equal(userProfile.FamilyName, result.Value.FamilyName);
        }

        [Fact]
        public async Task GetUserProfileById_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var handler = new GetUserProfileByIdHandler(_context, _getByIdLogger.Object);
            var query = new GetUserProfileByIdQuery
            {
                UserProfileId = Guid.NewGuid()
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task GetUserProfileByUserId_WithValidUserId_ShouldReturnProfile()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var userProfile = new UserProfile
            {
                Id = user.Id, // UserProfile ID matches User ID
                GivenName = "User",
                FamilyName = "Profile",
                DisplayName = "userprofile",
                Title = "Test",
                Description = "Test profile by user ID"
            };
            
            _context.Resources.Add(userProfile);
            await _context.SaveChangesAsync();

            var handler = new GetUserProfileByUserIdHandler(_context, _getByUserIdLogger.Object);
            var query = new GetUserProfileByUserIdQuery
            {
                UserId = user.Id
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(userProfile.GivenName, result.Value.GivenName);
            Assert.Equal(userProfile.FamilyName, result.Value.FamilyName);
        }

        [Fact]
        public async Task GetUserProfileByUserId_WithNonExistentUserId_ShouldReturnNotFound()
        {
            // Arrange
            var handler = new GetUserProfileByUserIdHandler(_context, _getByUserIdLogger.Object);
            var query = new GetUserProfileByUserIdQuery
            {
                UserId = Guid.NewGuid()
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task GetAllUserProfilesHandler_ShouldReturnAllProfiles()
        {
            // Arrange
            var user1 = await CreateTestUserAsync();
            var user2 = await CreateTestUserAsync();
            
            var userProfile1 = new UserProfile
            {
                Id = user1.Id,
                GivenName = "User",
                FamilyName = "One",
                DisplayName = "userone",
                Title = "Developer",
                Description = "First user profile"
            };
            
            var userProfile2 = new UserProfile
            {
                Id = user2.Id,
                GivenName = "User",
                FamilyName = "Two",
                DisplayName = "usertwo",
                Title = "Designer",
                Description = "Second user profile"
            };
            
            _context.Resources.AddRange(userProfile1, userProfile2);
            await _context.SaveChangesAsync();

            var handler = new GetAllUserProfilesHandler(_context, _getAllLogger.Object);
            var query = new GetAllUserProfilesQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public async Task SearchUserProfilesHandler_WithSearchTerm_ShouldReturnMatchingProfiles()
        {
            // Arrange
            var user1 = await CreateTestUserAsync();
            var user2 = await CreateTestUserAsync();
            
            var developerProfile = new UserProfile
            {
                Id = user1.Id,
                GivenName = "John",
                FamilyName = "Developer",
                DisplayName = "johndeveloper",
                Title = "Senior Developer",
                Description = "Experienced developer"
            };
            
            var designerProfile = new UserProfile
            {
                Id = user2.Id,
                GivenName = "Jane",
                FamilyName = "Designer",
                DisplayName = "janedesigner",
                Title = "UX Designer",
                Description = "Creative designer"
            };
            
            _context.Resources.AddRange(developerProfile, designerProfile);
            await _context.SaveChangesAsync();

            var handler = new SearchUserProfilesHandler(_context, _searchLogger.Object);
            var query = new SearchUserProfilesQuery
            {
                SearchTerm = "Developer",
                Take = 10,
                Skip = 0
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Contains("Developer", result.Value.First().Title);
        }

        [Fact]
        public async Task GetUserProfileStatisticsHandler_ShouldReturnCorrectStatistics()
        {
            // Arrange
            var user1 = await CreateTestUserAsync();
            var user2 = await CreateTestUserAsync();
            var user3 = await CreateTestUserAsync();
            
            var activeProfile1 = new UserProfile
            {
                Id = user1.Id,
                GivenName = "Active",
                FamilyName = "User1",
                DisplayName = "activeuser1",
                Title = "Developer",
                Description = "Active user profile"
            };
            
            var activeProfile2 = new UserProfile
            {
                Id = user2.Id,
                GivenName = "Active",
                FamilyName = "User2",
                DisplayName = "activeuser2",
                Title = "Designer",
                Description = "Another active user profile"
            };
            
            var deletedProfile = new UserProfile
            {
                Id = user3.Id,
                GivenName = "Deleted",
                FamilyName = "User",
                DisplayName = "deleteduser",
                Title = "Tester",
                Description = "Deleted user profile",
                DeletedAt = DateTime.UtcNow
            };
            
            _context.Resources.AddRange(activeProfile1, activeProfile2, deletedProfile);
            await _context.SaveChangesAsync();

            var handler = new GetUserProfileStatisticsHandler(_context, _statisticsLogger.Object);
            var query = new GetUserProfileStatisticsQuery { IncludeDeleted = true };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(3, result.Value.TotalUserProfiles);
            Assert.Equal(2, result.Value.ActiveUserProfiles);
            Assert.Equal(1, result.Value.DeletedUserProfiles);
        }

        [Fact]
        public async Task BulkDeleteUserProfilesHandler_WithValidIds_ShouldDeleteAllProfiles()
        {
            // Arrange
            var user1 = await CreateTestUserAsync();
            var user2 = await CreateTestUserAsync();
            var user3 = await CreateTestUserAsync();
            
            var profiles = new[]
            {
                new UserProfile
                {
                    Id = user1.Id,
                    GivenName = "User",
                    FamilyName = "One",
                    DisplayName = "userone",
                    Title = "Developer",
                    Description = "First profile to delete"
                },
                new UserProfile
                {
                    Id = user2.Id,
                    GivenName = "User",
                    FamilyName = "Two",
                    DisplayName = "usertwo",
                    Title = "Designer",
                    Description = "Second profile to delete"
                },
                new UserProfile
                {
                    Id = user3.Id,
                    GivenName = "User",
                    FamilyName = "Three",
                    DisplayName = "userthree",
                    Title = "Tester",
                    Description = "Third profile to delete"
                }
            };
            
            _context.Resources.AddRange(profiles);
            await _context.SaveChangesAsync();

            var handler = new BulkDeleteUserProfilesHandler(_context, _bulkDeleteLogger.Object);
            var command = new BulkDeleteUserProfilesCommand
            {
                UserProfileIds = profiles.Select(p => p.Id).ToList()
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value);
            
            // Verify profiles are soft deleted
            var deletedProfiles = await _context.Resources.OfType<UserProfile>()
                .Where(p => command.UserProfileIds.Contains(p.Id))
                .ToListAsync();
            Assert.All(deletedProfiles, profile => Assert.NotNull(profile.DeletedAt));
        }

        [Fact]
        public async Task BulkRestoreUserProfilesHandler_WithValidIds_ShouldRestoreAllProfiles()
        {
            // Arrange
            var user1 = await CreateTestUserAsync();
            var user2 = await CreateTestUserAsync();
            
            var profiles = new[]
            {
                new UserProfile
                {
                    Id = user1.Id,
                    GivenName = "Deleted",
                    FamilyName = "User1",
                    DisplayName = "deleteduser1",
                    Title = "Developer",
                    Description = "First deleted profile",
                    DeletedAt = DateTime.UtcNow
                },
                new UserProfile
                {
                    Id = user2.Id,
                    GivenName = "Deleted",
                    FamilyName = "User2",
                    DisplayName = "deleteduser2",
                    Title = "Designer",
                    Description = "Second deleted profile",
                    DeletedAt = DateTime.UtcNow
                }
            };
            
            _context.Resources.AddRange(profiles);
            await _context.SaveChangesAsync();

            var handler = new BulkRestoreUserProfilesHandler(_context, _bulkRestoreLogger.Object);
            var command = new BulkRestoreUserProfilesCommand
            {
                UserProfileIds = profiles.Select(p => p.Id).ToList()
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value);
            
            // Verify profiles are restored
            var restoredProfiles = await _context.Resources.OfType<UserProfile>()
                .IgnoreQueryFilters()
                .Where(p => command.UserProfileIds.Contains(p.Id))
                .ToListAsync();
            Assert.All(restoredProfiles, profile => Assert.Null(profile.DeletedAt));
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
