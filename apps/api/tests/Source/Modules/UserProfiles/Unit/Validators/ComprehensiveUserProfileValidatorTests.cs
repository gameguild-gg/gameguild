using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.UserProfiles;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Tests.Modules.UserProfiles.Unit.Validators {
  /// <summary>
  /// Comprehensive test suite for UserProfile validators
  /// Tests cover all validation rules and scenarios for Create and Update commands
  /// </summary>
  public class ComprehensiveUserProfileValidatorTests : IDisposable {
    private readonly ApplicationDbContext _context;
    private readonly CreateUserProfileCommandValidator _createValidator;
    private readonly UpdateUserProfileCommandValidator _updateValidator;

    public ComprehensiveUserProfileValidatorTests() {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;

      _context = new ApplicationDbContext(options);
      _createValidator = new CreateUserProfileCommandValidator(_context);
      _updateValidator = new UpdateUserProfileCommandValidator(_context);
    }

    #region Create UserProfile Validator Tests

    [Fact]
    public async Task CreateUserProfileValidator_WithValidData_ShouldPassValidation() {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "johndoe",
        Title = "Software Developer",
        Description = "Passionate developer",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null, "Given name is required")]
    [InlineData("", "Given name is required")]
    [InlineData(" ", "Given name is required")]
    public async Task CreateUserProfileValidator_WithInvalidGivenName_ShouldHaveValidationError(
        string givenName, string expectedError) {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = givenName,
        FamilyName = "Doe",
        DisplayName = "johndoe",
        Title = "Software Developer",
        Description = "Passionate developer",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.GivenName)
            .WithErrorMessage(expectedError);
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithGivenNameTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = new string('A', 101), // Exceeds 100 character limit
        FamilyName = "Doe",
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.GivenName)
            .WithErrorMessage("Given name must be between 1 and 100 characters");
    }

    [Theory]
    [InlineData("John123", "Given name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    [InlineData("John@Doe", "Given name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    [InlineData("John$Doe", "Given name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    public async Task CreateUserProfileValidator_WithInvalidGivenNameCharacters_ShouldHaveValidationError(
        string givenName, string expectedError) {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = givenName,
        FamilyName = "Doe",
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.GivenName)
            .WithErrorMessage(expectedError);
    }

    [Theory]
    [InlineData("John-Paul")]
    [InlineData("John Paul")]
    [InlineData("John'Paul")]
    [InlineData("John.Paul")]
    [InlineData("Mary-Jane Smith")]
    public async Task CreateUserProfileValidator_WithValidGivenNameCharacters_ShouldPassValidation(string givenName) {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = givenName,
        FamilyName = "Doe",
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldNotHaveValidationErrorFor(x => x.GivenName);
    }

    [Theory]
    [InlineData(null, "Family name is required")]
    [InlineData("", "Family name is required")]
    [InlineData(" ", "Family name is required")]
    public async Task CreateUserProfileValidator_WithInvalidFamilyName_ShouldHaveValidationError(
        string familyName, string expectedError) {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = "John",
        FamilyName = familyName,
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.FamilyName)
            .WithErrorMessage(expectedError);
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithFamilyNameTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = "John",
        FamilyName = new string('D', 101), // Exceeds 100 character limit
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.FamilyName)
            .WithErrorMessage("Family name must be between 1 and 100 characters");
    }

    [Theory]
    [InlineData(null, "Display name is required")]
    [InlineData("", "Display name is required")]
    [InlineData(" ", "Display name is required")]
    [InlineData("a", "Display name must be between 2 and 100 characters")]
    public async Task CreateUserProfileValidator_WithInvalidDisplayName_ShouldHaveValidationError(
        string displayName, string expectedError) {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = displayName,
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage(expectedError);
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithDisplayNameTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = new string('j', 101), // Exceeds 100 character limit
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must be between 2 and 100 characters");
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithDuplicateDisplayName_ShouldHaveValidationError() {
      // Arrange
      var user1 = await CreateTestUserAsync();
      var user2 = await CreateTestUserAsync();

      // Create existing profile with display name
      var existingProfile = new UserProfile {
        Id = user1.Id,
        GivenName = "Jane",
        FamilyName = "Smith",
        DisplayName = "uniquedisplay",
        Title = "",
        Description = "Existing user",
      };
      _context.Resources.Add(existingProfile);
      await _context.SaveChangesAsync();

      var command = new CreateUserProfileCommand {
        UserId = user2.Id,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "uniquedisplay", // Same as existing profile
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must be unique");
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithDeletedProfileDisplayName_ShouldPassValidation() {
      // Arrange
      var user1 = await CreateTestUserAsync();
      var user2 = await CreateTestUserAsync();

      // Create deleted profile with display name
      var deletedProfile = new UserProfile {
        Id = user1.Id,
        GivenName = "Jane",
        FamilyName = "Smith",
        DisplayName = "deleteddisplay",
        Title = "",
        Description = "Deleted user",
      };
      deletedProfile.SoftDelete();
      _context.Resources.Add(deletedProfile);
      await _context.SaveChangesAsync();

      var command = new CreateUserProfileCommand {
        UserId = user2.Id,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "deleteddisplay", // Same as deleted profile - should be allowed
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithTitleTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "johndoe",
        Title = new string('T', 201), // Exceeds 200 character limit
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 200 characters");
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithDescriptionTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var command = new CreateUserProfileCommand {
        UserId = user.Id,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "johndoe",
        Description = new string('D', 1001), // Exceeds 1000 character limit
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 1000 characters");
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithEmptyUserId_ShouldHaveValidationError() {
      // Arrange
      var command = new CreateUserProfileCommand {
        UserId = Guid.Empty,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithNonExistentUser_ShouldHaveValidationError() {
      // Arrange
      var command = new CreateUserProfileCommand {
        UserId = Guid.NewGuid(), // Non-existent user
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User does not exist");
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithUserWhoAlreadyHasProfile_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();

      // Create existing profile for user
      var existingProfile = new UserProfile {
        Id = user.Id,
        GivenName = "Jane",
        FamilyName = "Smith",
        DisplayName = "janesmith",
        Title = "",
        Description = "Existing profile",
      };
      _context.Resources.Add(existingProfile);
      await _context.SaveChangesAsync();

      var command = new CreateUserProfileCommand {
        UserId = user.Id, // User already has a profile
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User already has a profile");
    }

    [Fact]
    public async Task CreateUserProfileValidator_WithDeletedUser_ShouldHaveValidationError() {
      // Arrange
      var deletedUser = new User {
        Name = "Deleted User",
        Email = "deleted@example.com",
        IsActive = false,
        Balance = 0m,
        AvailableBalance = 0m,
      };
      deletedUser.SoftDelete();

      _context.Users.Add(deletedUser);
      await _context.SaveChangesAsync();

      var command = new CreateUserProfileCommand {
        UserId = deletedUser.Id,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "johndoe",
      };

      // Act
      var result = await _createValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User does not exist");
    }

    #endregion

    #region Update UserProfile Validator Tests

    [Fact]
    public async Task UpdateUserProfileValidator_WithValidData_ShouldPassValidation() {
      // Arrange
      var user = await CreateTestUserAsync();
      var profile = await CreateTestUserProfileAsync(user.Id);

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile.Id,
        GivenName = "John Updated",
        FamilyName = "Doe Updated",
        DisplayName = "johndoe_updated",
        Title = "Senior Developer",
        Description = "Senior software developer",
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithEmptyUserProfileId_ShouldHaveValidationError() {
      // Arrange
      var command = new UpdateUserProfileCommand {
        UserProfileId = Guid.Empty,
        GivenName = "John",
        FamilyName = "Doe",
        DisplayName = "johndoe",
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.UserProfileId)
            .WithErrorMessage("User profile ID is required");
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithPartialUpdate_ShouldPassValidation() {
      // Arrange
      var user = await CreateTestUserAsync();
      var profile = await CreateTestUserProfileAsync(user.Id);

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile.Id,
        GivenName = "John Updated",
        // Only updating given name, others are null/not provided
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithGivenNameTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var profile = await CreateTestUserProfileAsync(user.Id);

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile.Id,
        GivenName = new string('A', 101), // Exceeds 100 character limit
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.GivenName)
            .WithErrorMessage("Given name must be between 1 and 100 characters");
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithFamilyNameTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var profile = await CreateTestUserProfileAsync(user.Id);

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile.Id,
        FamilyName = new string('D', 101), // Exceeds 100 character limit
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.FamilyName)
            .WithErrorMessage("Family name must be between 1 and 100 characters");
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithDisplayNameTooShort_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var profile = await CreateTestUserProfileAsync(user.Id);

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile.Id,
        DisplayName = "a", // Too short (minimum 2 characters)
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must be between 2 and 100 characters");
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithDuplicateDisplayName_ShouldHaveValidationError() {
      // Arrange
      var user1 = await CreateTestUserAsync();
      var user2 = await CreateTestUserAsync();
      var profile1 = await CreateTestUserProfileAsync(user1.Id, "existing");
      var profile2 = await CreateTestUserProfileAsync(user2.Id, "updating");

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile2.Id,
        DisplayName = "existing", // Same as profile1's display name
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must be unique");
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithSameDisplayName_ShouldPassValidation() {
      // Arrange
      var user = await CreateTestUserAsync();
      var profile = await CreateTestUserProfileAsync(user.Id, "samedisplay");

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile.Id,
        DisplayName = "samedisplay", // Same as current display name - should be allowed
        GivenName = "Updated Name",
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithTitleTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var profile = await CreateTestUserProfileAsync(user.Id);

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile.Id,
        Title = new string('T', 201), // Exceeds 200 character limit
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 200 characters");
    }

    [Fact]
    public async Task UpdateUserProfileValidator_WithDescriptionTooLong_ShouldHaveValidationError() {
      // Arrange
      var user = await CreateTestUserAsync();
      var profile = await CreateTestUserProfileAsync(user.Id);

      var command = new UpdateUserProfileCommand {
        UserProfileId = profile.Id,
        Description = new string('D', 1001), // Exceeds 1000 character limit
      };

      // Act
      var result = await _updateValidator.TestValidateAsync(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 1000 characters");
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateTestUserAsync() {
      var user = new User {
        Name = $"Test User {Guid.NewGuid():N}",
        Email = $"user{Guid.NewGuid():N}@example.com",
        IsActive = true,
        Balance = 100m,
        AvailableBalance = 100m,
      };

      _context.Users.Add(user);
      await _context.SaveChangesAsync();
      return user;
    }

    private async Task<UserProfile> CreateTestUserProfileAsync(Guid userId, string? displayNameSuffix = null) {
      var displayName = displayNameSuffix ?? Guid.NewGuid().ToString("N")[..10];
      var profile = new UserProfile {
        Id = userId,
        GivenName = "Test",
        FamilyName = "User",
        DisplayName = displayName,
        Title = "Test Title",
        Description = "Test Description",
      };

      _context.Resources.Add(profile);
      await _context.SaveChangesAsync();
      return profile;
    }

    #endregion

    public void Dispose() {
      _context.Dispose();
    }
  }
}
