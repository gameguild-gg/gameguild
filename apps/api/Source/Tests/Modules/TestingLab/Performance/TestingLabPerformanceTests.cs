using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using GameGuild.Data;
using GameGuild.Modules.TestingLab.Services;
using GameGuild.Modules.TestingLab.Models;
using GameGuild.Modules.Users.Models;
using ProjectModel = GameGuild.Modules.Projects.Models.Project;
using ProjectVersionModel = GameGuild.Modules.Projects.Models.ProjectVersion;


namespace GameGuild.Tests.Modules.TestingLab.Performance;

/// <summary>
/// Performance tests for TestingLab operations
/// Tests response times and scalability under load
/// </summary>
public class TestingLabPerformanceTests : IDisposable {
  private readonly ApplicationDbContext _context;
  private readonly TestService _testService;

  public TestingLabPerformanceTests() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase($"TestingLabPerformanceDb_{Guid.NewGuid()}")
                  .Options;

    _context = new ApplicationDbContext(options);
    _testService = new TestService(_context);
  }

  [Fact]
  public async Task CreateTestingRequest_Performance_ShouldCompleteUnder100ms() {
    // Arrange
    var user = await CreateTestUserAsync();
    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);

    var testingRequest = new TestingRequest {
      ProjectVersionId = projectVersion.Id,
      Title = "Performance Test Request",
      Description = "Testing request creation performance",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Test instructions",
      MaxTesters = 5,
      StartDate = DateTime.UtcNow.AddDays(1),
      EndDate = DateTime.UtcNow.AddDays(7),
      Status = TestingRequestStatus.Open,
      CreatedById = user.Id,
    };

    var stopwatch = new Stopwatch();

    // Act
    stopwatch.Start();
    var result = await _testService.CreateTestingRequestAsync(testingRequest);
    stopwatch.Stop();

    // Assert
    Assert.NotNull(result);
    Assert.True(
      stopwatch.ElapsedMilliseconds < 200,
      $"Testing request creation took {stopwatch.ElapsedMilliseconds}ms, expected under 200ms"
    );
  }

  [Fact]
  public async Task GetAllTestingRequests_Performance_ShouldCompleteUnder500msFor1000Requests() {
    // Arrange
    var user = await CreateTestUserAsync();
    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);

    // Create 1000 testing requests
    var requests = new List<TestingRequest>();

    for (var i = 0; i < 1000; i++) {
      var request = new TestingRequest {
        ProjectVersionId = projectVersion.Id,
        Title = $"Performance Test Request {i}",
        Description = $"Performance testing request {i}",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "Test instructions",
        MaxTesters = 5,
        StartDate = DateTime.UtcNow.AddDays(1),
        EndDate = DateTime.UtcNow.AddDays(7),
        Status = TestingRequestStatus.Open,
        CreatedById = user.Id,
      };

      requests.Add(request);
    }

    _context.TestingRequests.AddRange(requests);
    await _context.SaveChangesAsync();

    var stopwatch = new Stopwatch();

    // Act
    stopwatch.Start();
    var result = await _testService.GetTestingRequestsAsync(0, 1000);
    stopwatch.Stop();

    // Assert
    Assert.Equal(1000, result.Count());
    Assert.True(
      stopwatch.ElapsedMilliseconds < 1000,
      $"Getting 1000 testing requests took {stopwatch.ElapsedMilliseconds}ms, expected under 1000ms"
    );
  }

  [Fact]
  public async Task SearchTestingRequests_Performance_ShouldCompleteUnder200msFor1000Requests() {
    // Arrange
    var user = await CreateTestUserAsync();
    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);

    // Create 1000 testing requests with searchable content
    var requests = new List<TestingRequest>();

    for (var i = 0; i < 1000; i++) {
      var request = new TestingRequest {
        ProjectVersionId = projectVersion.Id,
        Title = i % 10 == 0 ? $"Unity Game Test {i}" : $"Performance Test Request {i}",
        Description = $"Performance testing request {i}",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "Test instructions",
        MaxTesters = 5,
        StartDate = DateTime.UtcNow.AddDays(1),
        EndDate = DateTime.UtcNow.AddDays(7),
        Status = TestingRequestStatus.Open,
        CreatedById = user.Id,
      };

      requests.Add(request);
    }

    _context.TestingRequests.AddRange(requests);
    await _context.SaveChangesAsync();

    var stopwatch = new Stopwatch();

    // Act
    stopwatch.Start();
    var result = await _testService.SearchTestingRequestsAsync("Unity Game");
    stopwatch.Stop();

    // Assert
    Assert.True(result.Any());
    Assert.True(
      stopwatch.ElapsedMilliseconds < 500,
      $"Searching 1000 testing requests took {stopwatch.ElapsedMilliseconds}ms, expected under 500ms"
    );
  }

  [Fact]
  public async Task CreateTestingSession_Performance_ShouldCompleteUnder100ms() {
    // Arrange
    var user = await CreateTestUserAsync();
    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);
    var testingRequest = await CreateTestRequestAsync(projectVersion.Id, user.Id);
    var location = await CreateTestLocationAsync();

    var testingSession = new TestingSession {
      TestingRequestId = testingRequest.Id,
      LocationId = location.Id,
      SessionName = "Performance Test Session",
      SessionDate = DateTime.UtcNow.AddDays(2),
      StartTime = DateTime.UtcNow.AddDays(2).AddHours(9),
      EndTime = DateTime.UtcNow.AddDays(2).AddHours(17),
      MaxTesters = 10,
      Status = SessionStatus.Scheduled,
      ManagerUserId = user.Id,
      CreatedById = user.Id,
    };

    var stopwatch = new Stopwatch();

    // Act
    stopwatch.Start();
    var result = await _testService.CreateTestingSessionAsync(testingSession);
    stopwatch.Stop();

    // Assert
    Assert.NotNull(result);
    Assert.True(
      stopwatch.ElapsedMilliseconds < 200,
      $"Testing session creation took {stopwatch.ElapsedMilliseconds}ms, expected under 200ms"
    );
  }

  [Fact]
  public async Task GetTestingRequestStatistics_Performance_ShouldCompleteUnder150ms() {
    // Arrange
    var user = await CreateTestUserAsync();
    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);
    var testingRequest = await CreateTestRequestAsync(projectVersion.Id, user.Id);

    // Add some test data for statistics
    for (var i = 0; i < 50; i++) {
      var participant = new TestingParticipant {
        TestingRequestId = testingRequest.Id,
        UserId = user.Id,
        StartedAt = DateTime.UtcNow.AddDays(-i),
        InstructionsAcknowledged = true,
        InstructionsAcknowledgedAt = DateTime.UtcNow.AddDays(-i).AddMinutes(5),
        CompletedAt = i % 3 == 0 ? DateTime.UtcNow.AddDays(-i).AddHours(2) : null,
      };

      _context.TestingParticipants.Add(participant);
    }

    await _context.SaveChangesAsync();

    var stopwatch = new Stopwatch();

    // Act
    stopwatch.Start();
    var result = await _testService.GetTestingRequestStatisticsAsync(testingRequest.Id);
    stopwatch.Stop();

    // Assert
    Assert.NotNull(result);
    Assert.True(
      stopwatch.ElapsedMilliseconds < 300,
      $"Getting testing request statistics took {stopwatch.ElapsedMilliseconds}ms, expected under 300ms"
    );
  }

  [Theory]
  [InlineData(100)]
  [InlineData(500)]
  [InlineData(1000)]
  public async Task BulkOperations_Performance_ShouldScaleLinearly(int requestCount) {
    // Arrange
    var user = await CreateTestUserAsync();
    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);

    var requests = new List<TestingRequest>();

    for (var i = 0; i < requestCount; i++) {
      var request = new TestingRequest {
        ProjectVersionId = projectVersion.Id,
        Title = $"Bulk Test Request {i}",
        Description = $"Bulk testing request {i}",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "Test instructions",
        MaxTesters = 5,
        StartDate = DateTime.UtcNow.AddDays(1),
        EndDate = DateTime.UtcNow.AddDays(7),
        Status = TestingRequestStatus.Open,
        CreatedById = user.Id,
      };

      requests.Add(request);
    }

    var stopwatch = new Stopwatch();

    // Act
    stopwatch.Start();
    _context.TestingRequests.AddRange(requests);
    await _context.SaveChangesAsync();
    stopwatch.Stop();

    // Assert
    var timePerRequest = (double)stopwatch.ElapsedMilliseconds / requestCount;
    Assert.True(
      timePerRequest < 5.0,
      $"Bulk operation took {timePerRequest}ms per request, expected under 5ms per request"
    );
  }

  [Fact]
  public async Task ConcurrentOperations_Performance_ShouldHandleConcurrentReads() {
    // Arrange
    var user = await CreateTestUserAsync();
    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);

    // Create some test data
    for (var i = 0; i < 100; i++) {
      var request = new TestingRequest {
        ProjectVersionId = projectVersion.Id,
        Title = $"Concurrent Test Request {i}",
        Description = $"Concurrent testing request {i}",
        InstructionsType = InstructionType.Text,
        InstructionsContent = "Test instructions",
        MaxTesters = 5,
        StartDate = DateTime.UtcNow.AddDays(1),
        EndDate = DateTime.UtcNow.AddDays(7),
        Status = TestingRequestStatus.Open,
        CreatedById = user.Id,
      };

      _context.TestingRequests.Add(request);
    }

    await _context.SaveChangesAsync();

    var stopwatch = new Stopwatch();

    // Act
    stopwatch.Start();
    var tasks = new List<Task<IEnumerable<TestingRequest>>>();

    for (var i = 0; i < 10; i++) tasks.Add(_testService.GetTestingRequestsAsync(0, 100));

    await Task.WhenAll(tasks);
    stopwatch.Stop();

    // Assert
    Assert.True(
      stopwatch.ElapsedMilliseconds < 2000,
      $"10 concurrent read operations took {stopwatch.ElapsedMilliseconds}ms, expected under 2000ms"
    );
    Assert.All(tasks, task => Assert.Equal(100, task.Result.Count()));
  }

  // Helper methods for creating test data
  private async Task<User> CreateTestUserAsync() {
    var user = new User { Id = Guid.NewGuid(), Name = "Performance Test User", Email = "performance@test.com", CreatedAt = DateTime.UtcNow };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return user;
  }

  private async Task<ProjectModel> CreateTestProjectAsync(Guid userId) {
    var project = new ProjectModel { Id = Guid.NewGuid(), Title = "Performance Test Project", CreatedById = userId, CreatedAt = DateTime.UtcNow };
    _context.Projects.Add(project);
    await _context.SaveChangesAsync();

    return project;
  }

  private async Task<ProjectVersionModel> CreateTestProjectVersionAsync(Guid projectId, Guid userId) {
    var version = new ProjectVersionModel {
      Id = Guid.NewGuid(),
      ProjectId = projectId,
      VersionNumber = "1.0.0",
      CreatedById = userId,
      CreatedAt = DateTime.UtcNow,
    };

    _context.Set<ProjectVersionModel>().Add(version);
    await _context.SaveChangesAsync();

    return version;
  }

  private async Task<TestingRequest> CreateTestRequestAsync(Guid projectVersionId, Guid userId) {
    var request = new TestingRequest {
      Id = Guid.NewGuid(),
      ProjectVersionId = projectVersionId,
      Title = "Helper Test Request",
      Description = "Helper testing request",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Test instructions",
      MaxTesters = 5,
      StartDate = DateTime.UtcNow.AddDays(1),
      EndDate = DateTime.UtcNow.AddDays(7),
      Status = TestingRequestStatus.Open,
      CreatedById = userId,
    };

    _context.TestingRequests.Add(request);
    await _context.SaveChangesAsync();

    return request;
  }

  private async Task<TestingLocation> CreateTestLocationAsync() {
    var location = new TestingLocation {
      Id = Guid.NewGuid(),
      Name = "Performance Test Location",
      MaxTestersCapacity = 20,
      MaxProjectsCapacity = 10,
      Status = LocationStatus.Active,
      CreatedAt = DateTime.UtcNow,
    };

    _context.TestingLocations.Add(location);
    await _context.SaveChangesAsync();

    return location;
  }

  public void Dispose() { _context?.Dispose(); }
}
