using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.API.Tests.Fixtures;
using GameGuild.Common.Application.Services;
using GameGuild.Data;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.TestingLab.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using ProjectModel = GameGuild.Modules.Projects.Models.Project;
using ProjectVersionModel = GameGuild.Modules.Projects.Models.ProjectVersion;
using TenantModel = GameGuild.Modules.Tenants.Models.Tenant;
using UserModel = GameGuild.Modules.Users.Models.User;


namespace GameGuild.API.Tests.Modules.TestingLab.E2E.GraphQL;

/// <summary>
/// Integration tests for TestingLab GraphQL operations
/// Tests GraphQL queries and mutations with real database
/// </summary>
public class TestingLabGraphQLTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private readonly ApplicationDbContext _context;
  private readonly IServiceScope _scope;
  private readonly ITestOutputHelper _output;

  public TestingLabGraphQLTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
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
  public async Task GraphQL_GetTestingRequests_ShouldReturnTestingRequests() {
    // Arrange
    var (testingRequest, user, tenant) = await SeedTestDataAsync();
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var query = @"
            query {
                testingRequests {
                    id
                    title
                    description
                    status
                    maxTesters
                    createdAt
                }
            }";

    var request = new { query };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"GraphQL Response: {responseContent}");

    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
    Assert.True(result.TryGetProperty("data", out var data));
    Assert.True(data.TryGetProperty("testingRequests", out var testingRequests));
    Assert.True(testingRequests.GetArrayLength() > 0);

    var firstRequest = testingRequests[0];
    Assert.True(firstRequest.TryGetProperty("id", out var id));
    Assert.Equal(testingRequest.Id.ToString(), id.GetString());
  }

  [Fact]
  public async Task GraphQL_GetTestingRequestById_ShouldReturnTestingRequest() {
    // Arrange
    var (testingRequest, user, tenant) = await SeedTestDataAsync();
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var query = $@"
            query {{
                testingRequest(id: ""{testingRequest.Id}"") {{
                    id
                    title
                    description
                    status
                    maxTesters
                    instructionsType
                    instructionsContent
                    startDate
                    endDate
                    createdAt
                }}
            }}";

    var request = new { query };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"GraphQL Response: {responseContent}");

    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
    Assert.True(result.TryGetProperty("data", out var data));
    Assert.True(data.TryGetProperty("testingRequest", out var testingRequestResult));

    Assert.True(testingRequestResult.TryGetProperty("id", out var id));
    Assert.Equal(testingRequest.Id.ToString(), id.GetString());

    Assert.True(testingRequestResult.TryGetProperty("title", out var title));
    Assert.Equal(testingRequest.Title, title.GetString());
  }

  [Fact]
  public async Task GraphQL_GetTestingSessions_ShouldReturnTestingSessions() {
    // Arrange
    var (testingSession, user, tenant) = await SeedTestSessionDataAsync();
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var query = @"
            query {
                testingSessions {
                    id
                    sessionName
                    sessionDate
                    status
                    maxTesters
                    createdAt
                }
            }";

    var request = new { query };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"GraphQL Response: {responseContent}");

    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
    Assert.True(result.TryGetProperty("data", out var data));
    Assert.True(data.TryGetProperty("testingSessions", out var testingSessions));
    Assert.True(testingSessions.GetArrayLength() > 0);

    var firstSession = testingSessions[0];
    Assert.True(firstSession.TryGetProperty("id", out var id));
    Assert.Equal(testingSession.Id.ToString(), id.GetString());
  }

  [Fact]
  public async Task GraphQL_CreateTestingRequest_ShouldCreateTestingRequest() {
    // Arrange
    var (user, tenant) = await SeedUserAndTenantAsync();
    await SetupPermissionsAsync(user.Id, tenant.Id);

    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var mutation = $@"
            mutation {{
                createTestingRequest(input: {{
                    projectVersionId: ""{projectVersion.Id}""
                    title: ""GraphQL Test Request""
                    description: ""Created via GraphQL mutation""
                    instructionsType: TEXT
                    instructionsContent: ""GraphQL test instructions""
                    maxTesters: 8
                    startDate: ""{DateTime.UtcNow.AddDays(1):O}""
                    endDate: ""{DateTime.UtcNow.AddDays(7):O}""
                    status: OPEN
                }}) {{
                    id
                    title
                    description
                    status
                    maxTesters
                }}
            }}";

    var request = new { query = mutation };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"GraphQL Response: {responseContent}");

    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
    Assert.True(result.TryGetProperty("data", out var data));
    Assert.True(data.TryGetProperty("createTestingRequest", out var createdRequest));

    Assert.True(createdRequest.TryGetProperty("title", out var title));
    Assert.Equal("GraphQL Test Request", title.GetString());
  }

  [Fact]
  public async Task GraphQL_UpdateTestingRequest_ShouldUpdateTestingRequest() {
    // Arrange
    var (testingRequest, user, tenant) = await SeedTestDataAsync();
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var mutation = $@"
            mutation {{
                updateTestingRequest(id: ""{testingRequest.Id}"", input: {{
                    title: ""Updated GraphQL Test Request""
                    description: ""Updated via GraphQL mutation""
                    maxTesters: 15
                }}) {{
                    id
                    title
                    description
                    maxTesters
                }}
            }}";

    var request = new { query = mutation };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"GraphQL Response: {responseContent}");

    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
    Assert.True(result.TryGetProperty("data", out var data));
    Assert.True(data.TryGetProperty("updateTestingRequest", out var updatedRequest));

    Assert.True(updatedRequest.TryGetProperty("title", out var title));
    Assert.Equal("Updated GraphQL Test Request", title.GetString());

    Assert.True(updatedRequest.TryGetProperty("maxTesters", out var maxTesters));
    Assert.Equal(15, maxTesters.GetInt32());
  }

  [Fact]
  public async Task GraphQL_CreateTestingSession_ShouldCreateTestingSession() {
    // Arrange
    var (testingRequest, user, tenant) = await SeedTestDataAsync();
    var location = await CreateTestLocationAsync();
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var mutation = $@"
            mutation {{
                createTestingSession(input: {{
                    testingRequestId: ""{testingRequest.Id}""
                    locationId: ""{location.Id}""
                    sessionName: ""GraphQL Test Session""
                    sessionDate: ""{DateTime.UtcNow.AddDays(2):O}""
                    startTime: ""{DateTime.UtcNow.AddDays(2).AddHours(9):O}""
                    endTime: ""{DateTime.UtcNow.AddDays(2).AddHours(17):O}""
                    maxTesters: 10
                    status: SCHEDULED
                    managerUserId: ""{user.Id}""
                }}) {{
                    id
                    sessionName
                    status
                    maxTesters
                }}
            }}";

    var request = new { query = mutation };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"GraphQL Response: {responseContent}");

    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
    Assert.True(result.TryGetProperty("data", out var data));
    Assert.True(data.TryGetProperty("createTestingSession", out var createdSession));

    Assert.True(createdSession.TryGetProperty("sessionName", out var sessionName));
    Assert.Equal("GraphQL Test Session", sessionName.GetString());
  }

  [Fact]
  public async Task GraphQL_Schema_ShouldIncludeTestingLabTypes() {
    // Arrange
    var query = @"
            query {
                __schema {
                    types {
                        name
                        kind
                    }
                }
            }";

    var request = new { query };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Schema Response: {responseContent}");

    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
    Assert.True(result.TryGetProperty("data", out var data));
    Assert.True(data.TryGetProperty("__schema", out var schema));
    Assert.True(schema.TryGetProperty("types", out var types));

    var typeNames = new List<string>();

    foreach (var type in types.EnumerateArray()) {
      if (type.TryGetProperty("name", out var name)) typeNames.Add(name.GetString());
    }

    // Verify that TestingLab types are included in the schema
    Assert.Contains("TestingRequest", typeNames);
    Assert.Contains("TestingSession", typeNames);
    Assert.Contains("TestingLocation", typeNames);
    Assert.Contains("TestingParticipant", typeNames);
  }

  // Helper Methods
  private async Task<(TestingRequest, UserModel, TenantModel)> SeedTestDataAsync() {
    var (user, tenant) = await SeedUserAndTenantAsync();
    await SetupPermissionsAsync(user.Id, tenant.Id);

    var project = await CreateTestProjectAsync(user.Id);
    var projectVersion = await CreateTestProjectVersionAsync(project.Id, user.Id);

    var testingRequest = new TestingRequest {
      Id = Guid.NewGuid(),
      ProjectVersionId = projectVersion.Id,
      Title = "GraphQL Test Request",
      Description = "Test request for GraphQL testing",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "GraphQL test instructions",
      MaxTesters = 5,
      StartDate = DateTime.UtcNow.AddDays(1),
      EndDate = DateTime.UtcNow.AddDays(7),
      Status = TestingRequestStatus.Open,
      CreatedById = user.Id,
      CreatedAt = DateTime.UtcNow,
    };

    _context.TestingRequests.Add(testingRequest);
    await _context.SaveChangesAsync();

    return (testingRequest, user, tenant);
  }

  private async Task<(TestingSession, UserModel, TenantModel)> SeedTestSessionDataAsync() {
    var (testingRequest, user, tenant) = await SeedTestDataAsync();
    var location = await CreateTestLocationAsync();

    var testingSession = new TestingSession {
      Id = Guid.NewGuid(),
      TestingRequestId = testingRequest.Id,
      LocationId = location.Id,
      SessionName = "GraphQL Test Session",
      SessionDate = DateTime.UtcNow.AddDays(2),
      StartTime = DateTime.UtcNow.AddDays(2).AddHours(9),
      EndTime = DateTime.UtcNow.AddDays(2).AddHours(17),
      MaxTesters = 5,
      Status = SessionStatus.Scheduled,
      ManagerUserId = user.Id,
      CreatedById = user.Id,
      CreatedAt = DateTime.UtcNow,
    };

    _context.TestingSessions.Add(testingSession);
    await _context.SaveChangesAsync();

    return (testingSession, user, tenant);
  }

  private async Task<(UserModel, TenantModel)> SeedUserAndTenantAsync() {
    var tenant = new TenantModel {
      Id = Guid.NewGuid(),
      Name = "GraphQL Test Tenant",
      Slug = "graphql-test-tenant",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
    };

    _context.Tenants.Add(tenant);

    var user = new UserModel {
      Id = Guid.NewGuid(),
      Name = "graphqluser",
      Email = "graphql@test.com",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
    };

    _context.Users.Add(user);

    await _context.SaveChangesAsync();

    return (user, tenant);
  }

  private async Task<ProjectModel> CreateTestProjectAsync(Guid userId) {
    var project = new ProjectModel {
      Id = Guid.NewGuid(),
      Title = "GraphQL Test Project",
      Description = "Test project for GraphQL testing",
      CreatedById = userId,
      CreatedAt = DateTime.UtcNow,
    };

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

  private async Task<TestingLocation> CreateTestLocationAsync() {
    var location = new TestingLocation {
      Id = Guid.NewGuid(),
      Name = "GraphQL Test Location",
      MaxTestersCapacity = 20,
      MaxProjectsCapacity = 10,
      Status = LocationStatus.Active,
      CreatedAt = DateTime.UtcNow,
    };

    _context.TestingLocations.Add(location);
    await _context.SaveChangesAsync();

    return location;
  }

  private async Task SetupPermissionsAsync(Guid userId, Guid tenantId) {
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();

    // Grant content type permissions
    await permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      "TestingRequest",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );

    await permissionService.GrantContentTypePermissionAsync(
      userId,
      tenantId,
      "TestingSession",
      [PermissionType.Read, PermissionType.Create, PermissionType.Edit, PermissionType.Delete]
    );
  }

  private Task<string> CreateJwtTokenForUserAsync(UserModel user, TenantModel tenant) {
    var jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

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
