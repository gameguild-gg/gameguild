using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GameGuild.API.Tests.Fixtures;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.TestingLab.Models;
using Microsoft.Extensions.DependencyInjection;
using ProjectModel = GameGuild.Modules.Projects.Models.Project;
using ProjectVersionModel = GameGuild.Modules.Projects.Models.ProjectVersion;
using TenantModel = GameGuild.Modules.Tenants.Tenant;
using UserModel = GameGuild.Modules.Users.User;


namespace GameGuild.API.Tests.Modules.TestingLab.E2E.API;

public class TestingControllerE2ETests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;
  private readonly HttpClient _client;

  public TestingControllerE2ETests(TestWebApplicationFactory factory) {
    _factory = factory;
    _client = _factory.CreateClient();
  }

  [Fact]
  public async Task GetTestingRequests_ReturnsOkResult() {
    // Arrange
    TestingRequest testingRequest;
    UserModel user;
    TenantModel tenant;

    using (var scope = _factory.Services.CreateScope()) {
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      (testingRequest, user, tenant) = await SeedTestDataAsync(context);
    }

    // Set up authentication
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync("/testing/requests");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var requests =
      JsonSerializer.Deserialize<TestingRequest[]>(
        content,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      );

    Assert.NotNull(requests);
    Assert.True(requests.Length > 0);
  }

  [Fact]
  public async Task GetTestingRequest_WithValidId_ReturnsOkResult() {
    // Arrange
    TestingRequest testingRequest;
    UserModel user;
    TenantModel tenant;

    using (var scope = _factory.Services.CreateScope()) {
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      (testingRequest, user, tenant) = await SeedTestDataAsync(context);
    }

    // Set up authentication
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync($"/testing/requests/{testingRequest.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

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
  public async Task GetTestingRequest_WithInvalidId_ReturnsNotFound() {
    // Arrange
    TestingRequest testingRequest;
    UserModel user;
    TenantModel tenant;

    using (var scope = _factory.Services.CreateScope()) {
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      (testingRequest, user, tenant) = await SeedTestDataAsync(context);
    }

    var invalidId = Guid.NewGuid();

    // Set up authentication
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync($"/testing/requests/{invalidId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetTestingSessions_ReturnsOkResult() {
    // Arrange
    TestingSession testingSession;
    UserModel user;
    TenantModel tenant;

    using (var scope = _factory.Services.CreateScope()) {
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      (testingSession, user, tenant) = await SeedTestSessionDataAsync(context);
    }

    // Set up authentication
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync("/testing/sessions");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var sessions =
      JsonSerializer.Deserialize<TestingSession[]>(
        content,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      );

    Assert.NotNull(sessions);
    Assert.True(sessions.Length > 0);
  }

  [Fact]
  public async Task GetTestingSession_WithValidId_ReturnsOkResult() {
    // Arrange
    TestingSession testingSession;
    UserModel user;
    TenantModel tenant;

    using (var scope = _factory.Services.CreateScope()) {
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      (testingSession, user, tenant) = await SeedTestSessionDataAsync(context);
    }

    // Set up authentication
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Act
    var response = await _client.GetAsync($"/testing/sessions/{testingSession.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var session = JsonSerializer.Deserialize<TestingSession>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(session);
    Assert.Equal(testingSession.Id, session.Id);
  }

  private async Task<(TestingRequest, UserModel, TenantModel)> SeedTestDataAsync(ApplicationDbContext context) {
    // Create test tenant
    var tenant = new TenantModel { Id = Guid.NewGuid(), Name = "Test Tenant", CreatedAt = DateTime.UtcNow };
    context.Tenants.Add(tenant);

    // Create test user
    var user = new UserModel { Id = Guid.NewGuid(), Name = "testuser", Email = "test@example.com", CreatedAt = DateTime.UtcNow };
    context.Users.Add(user);

    // Grant tenant permissions and content type permissions for TestingRequest
    await GrantTenantPermissionsAsync(
      context,
      user.Id,
      tenant.Id,
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );
    await GrantContentTypePermissionsAsync(
      context,
      user.Id,
      tenant.Id,
      "TestingRequest",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );
    await GrantContentTypePermissionsAsync(
      context,
      user.Id,
      tenant.Id,
      "TestingSession",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );

    // Create test project
    var project = new ProjectModel { Id = Guid.NewGuid(), Title = "Test Project", CreatedById = user.Id, CreatedAt = DateTime.UtcNow };
    context.Projects.Add(project);

    // Create test project version
    var projectVersion = new ProjectVersionModel {
      Id = Guid.NewGuid(),
      ProjectId = project.Id,
      VersionNumber = "1.0.0",
      CreatedById = user.Id,
      CreatedAt = DateTime.UtcNow,
    };

    // ProjectVersion is accessed through Project.Versions, not a separate DbSet
    project.Versions.Add(projectVersion);

    // Create test testing request
    var testingRequest = new TestingRequest {
      Id = Guid.NewGuid(),
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
      CreatedAt = DateTime.UtcNow,
    };

    context.TestingRequests.Add(testingRequest);

    await context.SaveChangesAsync();

    return (testingRequest, user, tenant);
  }

  private async Task<(TestingSession, UserModel, TenantModel)> SeedTestSessionDataAsync(
    ApplicationDbContext context
  ) {
    var (testingRequest, user, tenant) = await SeedTestDataAsync(context);

    // Create test location
    var location = new TestingLocation {
      Id = Guid.NewGuid(),
      Name = "Test Location",
      MaxTestersCapacity = 10,
      MaxProjectsCapacity = 5,
      Status = LocationStatus.Active,
      CreatedAt = DateTime.UtcNow,
    };

    context.TestingLocations.Add(location);

    // Create test session
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

    context.TestingSessions.Add(testingSession);

    await context.SaveChangesAsync();

    return (testingSession, user, tenant);
  }

  private Task<string> CreateJwtTokenForUserAsync(UserModel user, TenantModel tenant) {
    // Get JWT service from the factory's services, not our custom scope
    var jwtService = _factory.Services.GetRequiredService<IJwtTokenService>();

    var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };

    var roles = new[] { "User" };

    var additionalClaims = new[] { new System.Security.Claims.Claim("tenant_id", tenant.Id.ToString()) };

    return Task.FromResult(jwtService.GenerateAccessToken(userDto, roles, additionalClaims));
  }

  private async Task GrantTenantPermissionsAsync(
    ApplicationDbContext context, Guid userId, Guid tenantId,
    PermissionType[] permissions
  ) {
    // Use the permission service from the factory's DI container
    using var scope = _factory.Services.CreateScope();
    var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantTenantPermissionAsync(userId, tenantId, permissions);
  }

  private async Task GrantContentTypePermissionsAsync(
    ApplicationDbContext context, Guid userId, Guid tenantId,
    string contentType, PermissionType[] permissions
  ) {
    // Use the permission service from the factory's DI container
    using var scope = _factory.Services.CreateScope();
    var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantContentTypePermissionAsync(userId, tenantId, contentType, permissions);
  }

  private void SetAuthorizationHeader(string token) { _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); }

  private void ClearAuthorizationHeader() { _client.DefaultRequestHeaders.Authorization = null; }

  public void Dispose() { _client.Dispose(); }
}
