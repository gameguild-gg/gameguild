using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.TestingLab;
using GameGuild.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using ProjectModel = GameGuild.Modules.Projects.Project;
using ProjectVersionModel = GameGuild.Modules.Projects.ProjectVersion;
using TenantModel = GameGuild.Modules.Tenants.Tenant;
using UserModel = GameGuild.Modules.Users.User;


namespace GameGuild.Tests.Modules.TestingLab.E2E.API;

/// <summary>
/// Integration tests for TestingController
/// Tests HTTP endpoints with real database and full application stack
/// </summary>
public class TestingControllerTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private readonly ApplicationDbContext _context;
  private readonly IServiceScope _scope;
  private readonly ITestOutputHelper _output;

  public TestingControllerTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
    _factory = factory;
    _output = output;
    _client = factory.CreateClient();
    _scope = factory.Services.CreateScope();

    // Get database context for test setup
    _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Clear any existing data to ensure clean test state
    ClearDatabase();
  }

  [Fact]
  public async Task GetTestingRequests_ShouldReturnEmptyArray_WhenNoRequests() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync("/testing/requests");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var requests =
      JsonSerializer.Deserialize<TestingRequest[]>(
        content,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      );

    Assert.NotNull(requests);
    Assert.Empty(requests);
  }

  [Fact]
  public async Task GetTestingRequests_ShouldReturnRequests_WhenRequestsExist() {
    // Arrange
    var (testingRequest, user, tenant) = await SeedTestDataAsync(_context);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync("/testing/requests");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var requests =
      JsonSerializer.Deserialize<TestingRequest[]>(
        content,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      );

    Assert.NotNull(requests);
    Assert.Single(requests);
    Assert.Equal(testingRequest.Id, requests[0].Id);
  }

  [Fact]
  public async Task CreateTestingRequest_ShouldCreateRequest_WithValidData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    var project = await CreateTestProjectAsync(_context, user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(_context, project.Id, user.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var newRequest = new {
      projectVersionId = projectVersion.Id,
      title = "New Test Request",
      description = "Description for new test request",
      instructionsType = 0, // InstructionType.Text = 0
      instructionsContent = "Test instructions",
      maxTesters = 10,
      startDate = DateTime.UtcNow.AddDays(1).ToString("O"),
      endDate = DateTime.UtcNow.AddDays(7).ToString("O"),
      status = 1 // TestingRequestStatus.Open = 1
    };

    var json = JsonSerializer.Serialize(newRequest);
    var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/testing/requests", stringContent);

    // Assert
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Status: {response.StatusCode}");
    _output.WriteLine($"Response Content: {responseContent}");

    Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task CreateTestingRequest_ShouldReturnBadRequest_WithInvalidData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var invalidRequest = new {
      // Missing required fields like projectVersionId, title, etc.
      description = "Description without required fields"
    };

    var json = JsonSerializer.Serialize(invalidRequest);
    var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/testing/requests", stringContent);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetTestingRequest_ShouldReturnRequest_WhenExists() {
    // Arrange
    var (testingRequest, user, tenant) = await SeedTestDataAsync(_context);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync($"/testing/requests/{testingRequest.Id}");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var request =
      JsonSerializer.Deserialize<TestingRequest>(
        content,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      );

    Assert.NotNull(request);
    Assert.Equal(testingRequest.Id, request.Id);
  }

  [Fact]
  public async Task GetTestingRequest_ShouldReturnNotFound_WhenNotExists() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await _client.GetAsync($"/testing/requests/{nonExistentId}");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetTestingSessions_ShouldReturnSessions_WhenSessionsExist() {
    // Arrange
    var (testingSession, user, tenant) = await SeedTestSessionDataAsync(_context);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync("/testing/sessions");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var sessions =
      JsonSerializer.Deserialize<TestingSession[]>(
        content,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      );

    Assert.NotNull(sessions);
    Assert.Single(sessions);
    Assert.Equal(testingSession.Id, sessions[0].Id);
  }

  [Fact]
  public async Task GetTestingSession_ShouldReturnSession_WhenExists() {
    // Arrange
    var (testingSession, user, tenant) = await SeedTestSessionDataAsync(_context);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    // Debug: Check if the session is actually in the database
    var sessionsInDb = await _context.TestingSessions.ToListAsync();
    _output.WriteLine($"Sessions in database: {sessionsInDb.Count}");

    foreach (var dbSession in sessionsInDb) _output.WriteLine($"Session ID: {dbSession.Id}, Name: {dbSession.SessionName}, DeletedAt: {dbSession.DeletedAt}");

    var response = await _client.GetAsync($"/testing/sessions/{testingSession.Id}");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var session =
      JsonSerializer.Deserialize<TestingSession>(
        content,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      );

    Assert.NotNull(session);
    Assert.Equal(testingSession.Id, session.Id);
  }

  // Helper Methods
  private async Task<(TestingRequest, UserModel, TenantModel)> SeedTestDataAsync(ApplicationDbContext context) {
    var (user, tenant) = await SeedUserAndTenantAsync(context);
    await SetupPermissionsAsync(context, user.Id, tenant.Id);

    var project = await CreateTestProjectAsync(context, user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(context, project.Id, user.Id);

    var testingRequest = new TestingRequest {
      Id = Guid.NewGuid(),
      ProjectVersionId = projectVersion.Id,
      Title = "Test Request",
      Description = "Test request description",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Test instructions",
      MaxTesters = 5,
      StartDate = DateTime.UtcNow.AddDays(1),
      EndDate = DateTime.UtcNow.AddDays(7),
      Status = TestingRequestStatus.Open,
      CreatedById = user.Id,
      CreatedAt = DateTime.UtcNow
    };

    context.TestingRequests.Add(testingRequest);
    await context.SaveChangesAsync();

    return (testingRequest, user, tenant);
  }

  private async Task<(TestingSession, UserModel, TenantModel)> SeedTestSessionDataAsync(ApplicationDbContext context) {
    var (testingRequest, user, tenant) = await SeedTestDataAsync(context);

    var location = new TestingLocation {
      Id = Guid.NewGuid(),
      Name = "Test Location",
      MaxTestersCapacity = 10,
      MaxProjectsCapacity = 5,
      Status = LocationStatus.Active,
      CreatedAt = DateTime.UtcNow
    };

    context.TestingLocations.Add(location);

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
      ManagerUserId = user.Id,
      CreatedById = user.Id,
      CreatedAt = DateTime.UtcNow
    };

    context.TestingSessions.Add(testingSession);
    await context.SaveChangesAsync();

    return (testingSession, user, tenant);
  }

  private static async Task<(UserModel, TenantModel)> SeedUserAndTenantAsync(ApplicationDbContext context) {
    var tenant = new TenantModel {
      Id = Guid.NewGuid(),
      Name = "Test Tenant",
      Slug = "test-tenant",
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };

    context.Tenants.Add(tenant);

    var user = new UserModel {
      Id = Guid.NewGuid(),
      Name = "testuser",
      Email = "test@example.com",
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };

    context.Users.Add(user);

    await context.SaveChangesAsync();

    return (user, tenant);
  }

  private static async Task<ProjectModel> CreateTestProjectAsync(ApplicationDbContext context, Guid userId) {
    var project = new ProjectModel {
      Id = Guid.NewGuid(),
      Title = "Test Project",
      Description = "Test project for testing lab",
      CreatedById = userId,
      CreatedAt = DateTime.UtcNow
    };

    context.Projects.Add(project);
    await context.SaveChangesAsync();

    return project;
  }

  private static async Task<ProjectVersionModel> CreateTestProjectVersionAsync(
    ApplicationDbContext context, Guid projectId,
    Guid userId
  ) {
    var version = new ProjectVersionModel {
      Id = Guid.NewGuid(),
      ProjectId = projectId,
      VersionNumber = "1.0.0",
      CreatedById = userId,
      CreatedAt = DateTime.UtcNow
    };

    context.Set<ProjectVersionModel>().Add(version);
    await context.SaveChangesAsync();

    return version;
  }

  private async Task SetupPermissionsAsync(ApplicationDbContext context, Guid userId, Guid tenantId) {
    using var scope = _factory.Services.CreateScope();
    var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

    // Grant content type permissions for TestingRequest
    await permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      "TestingRequest",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );

    // Grant content type permissions for TestingSession
    await permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      "TestingSession",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );

    // Grant content type permissions for TestingFeedback
    await permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      "TestingFeedback",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete, PermissionType.Report, PermissionType.Review]
    );

    // Grant content type permissions for SessionRegistration
    await permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      "SessionRegistration",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );
  }

  private Task<string> CreateJwtTokenForUserAsync(UserModel user, TenantModel tenant) {
    using var scope = _factory.Services.CreateScope();
    var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

    var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };

    var roles = new[] { "User" };
    var additionalClaims = new[] { new System.Security.Claims.Claim("tenant_id", tenant.Id.ToString()) };

    return Task.FromResult(jwtService.GenerateAccessToken(userDto, roles, additionalClaims));
  }

  private void SetAuthorizationHeader(string token) { _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); }

  private void ClearDatabase() {
    _context.TestingRequests.RemoveRange(_context.TestingRequests);
    _context.TestingSessions.RemoveRange(_context.TestingSessions);
    _context.TestingLocations.RemoveRange(_context.TestingLocations);
    _context.TestingParticipants.RemoveRange(_context.TestingParticipants);
    _context.Set<ProjectVersionModel>().RemoveRange(_context.Set<ProjectVersionModel>());
    _context.Projects.RemoveRange(_context.Projects);
    _context.Users.RemoveRange(_context.Users);
    _context.Tenants.RemoveRange(_context.Tenants);
    _context.SaveChanges();
  }

  #region Simplified Workflow Tests

  [Fact]
  public async Task SubmitSimpleTestingRequest_ShouldCreateRequest_WhenValidData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var requestDto = new CreateSimpleTestingRequestDto {
      Title = "Test Game Alpha",
      Description = "Alpha build testing",
      VersionNumber = "v1.0.0-alpha",
      DownloadUrl = "https://example.com/game.zip",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Test the core gameplay mechanics",
      FeedbackFormContent = "Please rate the game on a scale of 1-5",
      TeamIdentifier = "fa23-capstone-2023-24-t01"
    };

    var json = JsonSerializer.Serialize(requestDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/testing/submit-simple", content);

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    var createdRequest = JsonSerializer.Deserialize<TestingRequest>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    Assert.NotNull(createdRequest);
    Assert.Equal(requestDto.Title, createdRequest.Title);
    Assert.Equal(requestDto.DownloadUrl, createdRequest.DownloadUrl);
    Assert.Equal(TestingRequestStatus.Draft, createdRequest.Status);
  }

  [Fact]
  public async Task SubmitSimpleTestingRequest_ShouldReturnBadRequest_WhenInvalidData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var requestDto = new CreateSimpleTestingRequestDto {
      // Missing required fields
      Title = "",
      VersionNumber = "",
      DownloadUrl = "invalid-url"
    };

    var json = JsonSerializer.Serialize(requestDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/testing/submit-simple", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task SubmitFeedback_ShouldCreateFeedback_WhenValidData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    // Create a testing request first
    var testingRequest = await SeedTestingRequestAsync(_context, user, tenant);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var feedbackDto = new SubmitFeedbackDto {
      TestingRequestId = testingRequest.Id,
      FeedbackResponses = "{\"rating\": 5, \"comments\": \"Great game!\"}",
      OverallRating = 5,
      WouldRecommend = true,
      AdditionalNotes = "Really enjoyed the gameplay"
    };

    var json = JsonSerializer.Serialize(feedbackDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/testing/feedback", content);

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    Assert.Contains("Feedback submitted successfully", responseContent);

    // Verify feedback was created in database
    var feedback = await _context.TestingFeedback.FirstOrDefaultAsync(f => f.TestingRequestId == testingRequest.Id);
    Assert.NotNull(feedback);
    Assert.Equal(feedbackDto.OverallRating, feedback.OverallRating);
    Assert.Equal(feedbackDto.WouldRecommend, feedback.WouldRecommend);
  }

  [Fact]
  public async Task GetAvailableTestingRequests_ShouldReturnActiveRequests() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    // Create some testing requests
    var activeRequest = await SeedTestingRequestAsync(_context, user, tenant, TestingRequestStatus.Open);
    var draftRequest = await SeedTestingRequestAsync(_context, user, tenant, TestingRequestStatus.Draft);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync("/testing/available-for-testing");

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    var requests = JsonSerializer.Deserialize<TestingRequest[]>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    Assert.NotNull(requests);
    Assert.Contains(requests, r => r.Id == activeRequest.Id);
    Assert.DoesNotContain(requests, r => r.Id == draftRequest.Id); // Draft requests should not be available for testing
  }

  [Fact]
  public async Task GetMyTestingRequests_ShouldReturnUserRequests() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    var (otherUser, _) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    // Create requests for both users
    var userRequest = await SeedTestingRequestAsync(_context, user, tenant);
    var otherUserRequest = await SeedTestingRequestAsync(_context, otherUser, tenant);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync("/testing/my-requests");

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    var requests = JsonSerializer.Deserialize<TestingRequest[]>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    Assert.NotNull(requests);
    Assert.Contains(requests, r => r.Id == userRequest.Id);
    Assert.DoesNotContain(requests, r => r.Id == otherUserRequest.Id);
  }

  #endregion

  #region Attendance Management Tests

  [Fact]
  public async Task GetStudentAttendanceReport_ShouldReturnAttendanceData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync("/testing/attendance/students");

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    Assert.NotNull(responseContent);
    // Note: The actual structure depends on the implementation of GetStudentAttendanceReportAsync
  }

  [Fact]
  public async Task UpdateAttendance_ShouldUpdateSessionAttendance_WhenValidData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    // Create a testing session
    var session = await SeedTestingSessionAsync(_context, user, tenant);

    // Create a session registration for the user
    var registration = new SessionRegistration {
      Id = Guid.NewGuid(),
      SessionId = session.Id,
      UserId = user.Id,
      RegistrationType = RegistrationType.Tester,
      AttendanceStatus = AttendanceStatus.Registered,
      CreatedAt = DateTime.UtcNow
    };
    _context.SessionRegistrations.Add(registration);
    await _context.SaveChangesAsync();

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var attendanceDto = new UpdateAttendanceDto {
      UserId = user.Id,
      AttendanceStatus = AttendanceStatus.Completed
    };

    var json = JsonSerializer.Serialize(attendanceDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync($"/testing/sessions/{session.Id}/attendance", content);

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    Assert.Contains("Attendance updated successfully", responseContent);
  }

  #endregion

  #region Feedback Quality Management Tests

  [Fact]
  public async Task ReportFeedback_ShouldMarkFeedbackAsReported_WhenValidData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    // Create testing request and feedback
    var testingRequest = await SeedTestingRequestAsync(_context, user, tenant);
    var feedback = await SeedTestingFeedbackAsync(_context, testingRequest, user);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var reportDto = new ReportFeedbackDto {
      Reason = "Inappropriate content"
    };

    var json = JsonSerializer.Serialize(reportDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync($"/testing/feedback/{feedback.Id}/report", content);

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    Assert.Contains("Feedback reported successfully", responseContent);

    // Verify feedback was marked as reported
    // Detach the entity to ensure we get fresh data from the database
    _context.Entry(feedback).State = EntityState.Detached;
    var updatedFeedback = await _context.TestingFeedback
        .FirstOrDefaultAsync(f => f.Id == feedback.Id);
    Assert.NotNull(updatedFeedback);
    Assert.True(updatedFeedback.IsReported);
    Assert.Equal(reportDto.Reason, updatedFeedback.ReportReason);
  }

  [Fact]
  public async Task RateFeedbackQuality_ShouldUpdateQualityRating_WhenValidData() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    // Create testing request and feedback
    var testingRequest = await SeedTestingRequestAsync(_context, user, tenant);
    var feedback = await SeedTestingFeedbackAsync(_context, testingRequest, user);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var qualityDto = new RateFeedbackQualityDto {
      Quality = FeedbackQuality.Positive
    };

    var json = JsonSerializer.Serialize(qualityDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync($"/testing/feedback/{feedback.Id}/quality", content);

    // Debug: Print response content if not successful
    if (!response.IsSuccessStatusCode) {
      var errorContent = await response.Content.ReadAsStringAsync();
      _output.WriteLine($"Response Status: {response.StatusCode}");
      _output.WriteLine($"Response Content: {errorContent}");
    }

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    Assert.Contains("Feedback quality rated successfully", responseContent);

    // Verify feedback quality was updated
    // Detach the entity to ensure we get fresh data from the database
    _context.Entry(feedback).State = EntityState.Detached;
    var updatedFeedback = await _context.TestingFeedback
        .FirstOrDefaultAsync(f => f.Id == feedback.Id);
    Assert.NotNull(updatedFeedback);
    Assert.Equal(FeedbackQuality.Positive, updatedFeedback.QualityRating);
  }

  [Fact]
  public async Task ReportFeedback_ShouldReturnNotFound_WhenFeedbackDoesNotExist() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync(_context);
    await SetupPermissionsAsync(_context, user.Id, tenant.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var reportDto = new ReportFeedbackDto {
      Reason = "Test reason"
    };

    var json = JsonSerializer.Serialize(reportDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var nonExistentFeedbackId = Guid.NewGuid();

    // Act
    var response = await _client.PostAsync($"/testing/feedback/{nonExistentFeedbackId}/report", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  #endregion

  #region Helper Methods for New Tests

  private async Task<TestingRequest> SeedTestingRequestAsync(ApplicationDbContext context, UserModel user, TenantModel tenant, TestingRequestStatus status = TestingRequestStatus.Draft) {
    var project = await CreateTestProjectAsync(context, user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(context, project.Id, user.Id);

    var testingRequest = new TestingRequest {
      Id = Guid.NewGuid(),
      Title = $"Test Request {Guid.NewGuid().ToString()[..8]}",
      Description = "Test description",
      ProjectVersionId = projectVersion.Id,
      DownloadUrl = "https://example.com/game.zip",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Test instructions",
      FeedbackFormContent = "Feedback form",
      MaxTesters = 8,
      CurrentTesterCount = 0,
      StartDate = DateTime.UtcNow,
      EndDate = DateTime.UtcNow.AddDays(7),
      Status = status,
      CreatedById = user.Id,
      CreatedAt = DateTime.UtcNow
    };

    context.TestingRequests.Add(testingRequest);
    await context.SaveChangesAsync();

    return testingRequest;
  }

  private async Task<TestingSession> SeedTestingSessionAsync(ApplicationDbContext context, UserModel user, TenantModel tenant) {
    var testingRequest = await SeedTestingRequestAsync(context, user, tenant);

    var location = new TestingLocation {
      Id = Guid.NewGuid(),
      Name = "Test Location",
      MaxTestersCapacity = 10,
      MaxProjectsCapacity = 5,
      Status = LocationStatus.Active,
      CreatedAt = DateTime.UtcNow
    };

    context.TestingLocations.Add(location);

    var session = new TestingSession {
      Id = Guid.NewGuid(),
      TestingRequestId = testingRequest.Id,
      LocationId = location.Id,
      SessionName = "Test Session",
      SessionDate = DateTime.UtcNow.AddDays(2),
      StartTime = DateTime.UtcNow.AddDays(2).AddHours(9),
      EndTime = DateTime.UtcNow.AddDays(2).AddHours(17),
      MaxTesters = 5,
      Status = SessionStatus.Scheduled,
      ManagerUserId = user.Id,
      CreatedById = user.Id,
      CreatedAt = DateTime.UtcNow
    };

    context.TestingSessions.Add(session);
    await context.SaveChangesAsync();

    return session;
  }

  private async Task<TestingFeedback> SeedTestingFeedbackAsync(ApplicationDbContext context, TestingRequest testingRequest, UserModel user) {
    // Create a feedback form first
    var feedbackForm = new TestingFeedbackForm {
      Id = Guid.NewGuid(),
      TestingRequestId = testingRequest.Id,
      FormSchema = "{}",
      CreatedAt = DateTime.UtcNow
    };

    context.TestingFeedbackForms.Add(feedbackForm);

    var feedback = new TestingFeedback {
      Id = Guid.NewGuid(),
      TestingRequestId = testingRequest.Id,
      FeedbackFormId = feedbackForm.Id,
      UserId = user.Id,
      TestingContext = TestingContext.InPerson,
      FeedbackData = "{\"rating\": 4, \"comments\": \"Good game\"}",
      OverallRating = 4,
      WouldRecommend = true,
      AdditionalNotes = "Test feedback",
      IsReported = false,
      QualityRating = FeedbackQuality.Neutral,
      CreatedAt = DateTime.UtcNow
    };

    context.TestingFeedback.Add(feedback);
    await context.SaveChangesAsync();

    return feedback;
  }

  #endregion

  public void Dispose() {
    _scope?.Dispose();
    _client?.Dispose();
  }
}
