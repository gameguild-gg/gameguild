using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.API.Tests.Fixtures;
using GameGuild.Common.Services;
using GameGuild.Data;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.TestingLab.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using ProjectModel = GameGuild.Modules.Projects.Models.Project;
using ProjectVersionModel = GameGuild.Modules.Projects.Models.ProjectVersion;
using TenantModel = GameGuild.Modules.Tenants.Models.Tenant;
using UserModel = GameGuild.Modules.Users.Models.User;


namespace GameGuild.API.Tests.Modules.TestingLab.E2E.API;

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
      status = 1, // TestingRequestStatus.Open = 1
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
      description = "Description without required fields",
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
      CreatedAt = DateTime.UtcNow,
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
      CreatedAt = DateTime.UtcNow,
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
      CreatedAt = DateTime.UtcNow,
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
      CreatedAt = DateTime.UtcNow,
    };

    context.Tenants.Add(tenant);

    var user = new UserModel {
      Id = Guid.NewGuid(),
      Name = "testuser",
      Email = "test@example.com",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
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
      CreatedAt = DateTime.UtcNow,
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
      CreatedAt = DateTime.UtcNow,
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

  public void Dispose() {
    _scope?.Dispose();
    _client?.Dispose();
  }
}
