using Microsoft.EntityFrameworkCore;
using Xunit;
using GameGuild.Data;
using GameGuild.Modules.TestingLab.Models;
using GameGuild.Modules.TestingLab.Services;
using GameGuild.Modules.User.Models;
using ProjectModel = GameGuild.Modules.Project.Models.Project;
using ProjectVersionModel = GameGuild.Modules.Project.Models.ProjectVersion;


namespace GameGuild.Tests.Modules.TestingLab.Unit.Services;

public class TestServiceTests : IDisposable {
  private readonly ApplicationDbContext _context;
  private readonly TestService _testService;

  public TestServiceTests() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                  .Options;

    _context = new ApplicationDbContext(options);
    _testService = new TestService(_context);
  }

  [Fact]
  public async Task CreateTestingRequestAsync_ValidRequest_ReturnsCreatedRequest() {
    // Arrange
    var user = await CreateTestUserAsync();
    var projectVersion = await CreateTestProjectVersionAsync(user.Id);

    var testingRequest = new TestingRequest {
      ProjectVersionId = projectVersion.Id,
      Title = "Test Request",
      Description = "Test Description",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Test instructions",
      MaxTesters = 5,
      StartDate = DateTime.UtcNow.AddDays(1),
      EndDate = DateTime.UtcNow.AddDays(7),
      Status = TestingRequestStatus.Open,
      CreatedById = user.Id,
    };

    // Act
    var result = await _testService.CreateTestingRequestAsync(testingRequest);

    // Assert
    Assert.NotNull(result);
    Assert.NotEqual(Guid.Empty, result.Id);
    Assert.Equal("Test Request", result.Title);
    Assert.Equal("Test Description", result.Description);
    Assert.Equal(TestingRequestStatus.Open, result.Status);
    Assert.Equal(user.Id, result.CreatedById);
  }

  [Fact]
  public async Task GetTestingRequestByIdAsync_ExistingRequest_ReturnsRequest() {
    // Arrange
    var testingRequest = await CreateTestingRequestAsync();

    // Act
    var result = await _testService.GetTestingRequestByIdAsync(testingRequest.Id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(testingRequest.Id, result.Id);
    Assert.Equal(testingRequest.Title, result.Title);
  }

  [Fact]
  public async Task GetTestingRequestByIdAsync_NonExistentRequest_ReturnsNull() {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var result = await _testService.GetTestingRequestByIdAsync(nonExistentId);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task UpdateTestingRequestAsync_ValidRequest_ReturnsUpdatedRequest() {
    // Arrange
    var testingRequest = await CreateTestingRequestAsync();
    testingRequest.Title = "Updated Title";
    testingRequest.Description = "Updated Description";

    // Act
    var result = await _testService.UpdateTestingRequestAsync(testingRequest);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Updated Title", result.Title);
    Assert.Equal("Updated Description", result.Description);
  }

  [Fact]
  public async Task DeleteTestingRequestAsync_ExistingRequest_SetsDeletedAt() {
    // Arrange
    var testingRequest = await CreateTestingRequestAsync();

    // Act
    var result = await _testService.DeleteTestingRequestAsync(testingRequest.Id);

    // Assert
    Assert.True(result);

    // Reload the entity from the database to see the updated DeletedAt value
    _context.ChangeTracker.Clear();
    var deletedRequest = await _context.TestingRequests.IgnoreQueryFilters()
                                       .FirstOrDefaultAsync(tr => tr.Id == testingRequest.Id);
    Assert.NotNull(deletedRequest);
    Assert.NotNull(deletedRequest.DeletedAt);
  }

  [Fact]
  public async Task CreateTestingSessionAsync_ValidSession_ReturnsCreatedSession() {
    // Arrange
    var testingRequest = await CreateTestingRequestAsync();
    var location = await CreateTestLocationAsync();

    var testingSession = new TestingSession {
      TestingRequestId = testingRequest.Id,
      LocationId = location.Id,
      SessionName = "Test Session",
      SessionDate = DateTime.UtcNow.AddDays(2),
      StartTime = DateTime.UtcNow.AddDays(2).AddHours(9),
      EndTime = DateTime.UtcNow.AddDays(2).AddHours(17),
      MaxTesters = 5,
      Status = SessionStatus.Scheduled,
      ManagerUserId = testingRequest.CreatedById,
      CreatedById = testingRequest.CreatedById,
    };

    // Act
    var result = await _testService.CreateTestingSessionAsync(testingSession);

    // Assert
    Assert.NotNull(result);
    Assert.NotEqual(Guid.Empty, result.Id);
    Assert.Equal("Test Session", result.SessionName);
    Assert.Equal(SessionStatus.Scheduled, result.Status);
    Assert.Equal(testingRequest.Id, result.TestingRequestId);
  }

  [Fact]
  public async Task AddParticipantAsync_ValidRequestAndUser_CreatesParticipant() {
    // Arrange
    var testingRequest = await CreateTestingRequestAsync();
    var user = await CreateTestUserAsync("participant@example.com");

    // Act
    var result = await _testService.AddParticipantAsync(testingRequest.Id, user.Id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(testingRequest.Id, result.TestingRequestId);
    Assert.Equal(user.Id, result.UserId);
  }

  [Fact]
  public async Task RegisterForSessionAsync_ValidSessionAndUser_CreatesRegistration() {
    // Arrange
    var testingSession = await CreateTestingSessionAsync();
    var user = await CreateTestUserAsync("registrant@example.com");

    // Act
    var result =
      await _testService.RegisterForSessionAsync(testingSession.Id, user.Id, RegistrationType.Tester, "Test notes");

    // Assert
    Assert.NotNull(result);
    Assert.Equal(testingSession.Id, result.SessionId);
    Assert.Equal(user.Id, result.UserId);
    Assert.Equal(RegistrationType.Tester, result.RegistrationType);
  }

  [Fact]
  public async Task AddFeedbackAsync_ValidData_CreatesFeedback() {
    // Arrange
    var testingRequest = await CreateTestingRequestAsync();
    var user = await CreateTestUserAsync("feedback@example.com");
    var feedbackForm = await CreateTestFeedbackFormAsync();

    // Act
    var result = await _testService.AddFeedbackAsync(
                   testingRequest.Id,
                   user.Id,
                   feedbackForm.Id,
                   "Test feedback data",
                   TestingContext.Online,
                   null,
                   "Additional notes"
                 );

    // Assert
    Assert.NotNull(result);
    Assert.Equal(testingRequest.Id, result.TestingRequestId);
    Assert.Equal(user.Id, result.UserId);
    Assert.Equal(feedbackForm.Id, result.FeedbackFormId);
  }

  [Fact]
  public async Task GetTestingRequestStatisticsAsync_ExistingRequest_ReturnsStatistics() {
    // Arrange
    var testingRequest = await CreateTestingRequestAsync();

    // Add some participants
    var user1 = await CreateTestUserAsync("user1@example.com");
    var user2 = await CreateTestUserAsync("user2@example.com");
    await _testService.AddParticipantAsync(testingRequest.Id, user1.Id);
    await _testService.AddParticipantAsync(testingRequest.Id, user2.Id);

    // Act
    var result = await _testService.GetTestingRequestStatisticsAsync(testingRequest.Id);

    // Assert
    Assert.NotNull(result);
    // Add specific assertions based on the statistics structure
  }

  [Fact]
  public async Task SearchTestingRequestsAsync_WithTerm_ReturnsMatchingRequests() {
    // Arrange
    await CreateTestingRequestAsync("Searchable Request");
    await CreateTestingRequestAsync("Another Request");

    // Act
    var result = await _testService.SearchTestingRequestsAsync("Searchable");

    // Assert
    Assert.NotNull(result);
    var resultList = result.ToList();
    Assert.Single(resultList);
    Assert.Contains("Searchable", resultList.First().Title);
  }

  // Helper methods
  private async Task<User> CreateTestUserAsync(string email = "test@example.com") {
    var user = new User { Id = Guid.NewGuid(), Name = email.Split('@')[0], Email = email, CreatedAt = DateTime.UtcNow };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return user;
  }

  private async Task<ProjectVersionModel> CreateTestProjectVersionAsync(Guid createdById) {
    var project = new ProjectModel { Id = Guid.NewGuid(), Title = "Test Project", CreatedById = createdById, CreatedAt = DateTime.UtcNow };
    _context.Projects.Add(project);

    var projectVersion = new ProjectVersionModel {
      Id = Guid.NewGuid(),
      ProjectId = project.Id,
      VersionNumber = "1.0.0",
      CreatedById = createdById,
      CreatedAt = DateTime.UtcNow,
    };

    // ProjectVersion is accessed through Project.Versions, not a separate DbSet
    project.Versions.Add(projectVersion);
    await _context.SaveChangesAsync();

    return projectVersion;
  }

  private async Task<TestingRequest> CreateTestingRequestAsync(string title = "Test Request") {
    var user = await CreateTestUserAsync();
    var projectVersion = await CreateTestProjectVersionAsync(user.Id);

    var testingRequest = new TestingRequest {
      Id = Guid.NewGuid(),
      ProjectVersionId = projectVersion.Id,
      Title = title,
      Description = "Test Description",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Test instructions",
      MaxTesters = 5,
      StartDate = DateTime.UtcNow.AddDays(1),
      EndDate = DateTime.UtcNow.AddDays(7),
      Status = TestingRequestStatus.Open,
      CreatedById = user.Id,
      CreatedAt = DateTime.UtcNow,
    };

    _context.TestingRequests.Add(testingRequest);
    await _context.SaveChangesAsync();

    return testingRequest;
  }

  private async Task<TestingLocation> CreateTestLocationAsync() {
    var location = new TestingLocation {
      Id = Guid.NewGuid(),
      Name = "Test Location",
      MaxTestersCapacity = 10,
      MaxProjectsCapacity = 5,
      Status = LocationStatus.Active,
      CreatedAt = DateTime.UtcNow,
    };

    _context.TestingLocations.Add(location);
    await _context.SaveChangesAsync();

    return location;
  }

  private async Task<TestingSession> CreateTestingSessionAsync() {
    var testingRequest = await CreateTestingRequestAsync();
    var location = await CreateTestLocationAsync();

    var testingSession = new TestingSession {
      Id = Guid.NewGuid(),
      TestingRequestId = testingRequest.Id,
      LocationId = location.Id,
      SessionName = "Test Session",
      SessionDate = DateTime.UtcNow.AddDays(2),
      StartTime = DateTime.UtcNow.AddDays(2).AddHours(9),
      EndTime = DateTime.UtcNow.AddDays(2).AddHours(17),
      MaxTesters = 5,
      Status = SessionStatus.Scheduled,
      ManagerUserId = testingRequest.CreatedById,
      CreatedById = testingRequest.CreatedById,
      CreatedAt = DateTime.UtcNow,
    };

    _context.TestingSessions.Add(testingSession);
    await _context.SaveChangesAsync();

    return testingSession;
  }

  private async Task<TestingFeedbackForm> CreateTestFeedbackFormAsync() {
    var testingRequest = await CreateTestingRequestAsync();

    var feedbackForm = new TestingFeedbackForm {
      Id = Guid.NewGuid(),
      TestingRequestId = testingRequest.Id,
      FormSchema = "{}",
      IsForOnline = true,
      IsForSessions = true,
      CreatedAt = DateTime.UtcNow,
    };

    _context.TestingFeedbackForms.Add(feedbackForm);
    await _context.SaveChangesAsync();

    return feedbackForm;
  }

  public void Dispose() {
    _context.Database.EnsureDeleted();
    _context.Dispose();
  }
}
