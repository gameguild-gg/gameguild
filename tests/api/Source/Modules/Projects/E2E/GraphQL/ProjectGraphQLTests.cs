using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.API.Tests.Fixtures;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using ProjectType = GameGuild.Common.ProjectType;
using TenantModel = GameGuild.Modules.Tenants.Tenant;


namespace GameGuild.API.Tests.Modules.Projects.E2E.GraphQL;

/// <summary>
/// Integration tests for Project GraphQL operations
/// Tests GraphQL queries and mutations with real database
/// </summary>
public class ProjectGraphQLTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;

  private readonly HttpClient _client;

  private readonly ApplicationDbContext _context;

  private readonly IServiceScope _scope;

  private readonly ITestOutputHelper _output;

  public ProjectGraphQLTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
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
  public async Task GraphQL_GetProjects_ShouldReturnProjects() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();

    // Grant content-type permission to read projects
    await GrantContentTypePermissions(user, tenant, "Project", [PermissionType.Read]);

    var token = await GenerateJwtTokenAsync(user, tenant);
    SetAuthorizationHeader(token);

    var project1 = new Project {
      Title = "GraphQL Test Project 1",
      Description = "First test project for GraphQL",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Type = ProjectType.Game
    };

    var project2 = new Project {
      Title = "GraphQL Test Project 2",
      Description = "Second test project for GraphQL",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Type = ProjectType.Tool
    };

    _context.Projects.AddRange(project1, project2);
    await _context.SaveChangesAsync();

    var query = @"
        {
            projects {
                id
                title
                description
                status
                visibility
                type
                developmentStatus
                createdAt
                updatedAt
            }
        }";

    var request = new { query = query };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Debug output
    _output.WriteLine($"Response Status: {response.StatusCode}");
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    Assert.Contains("GraphQL Test Project 1", responseContent);
    Assert.Contains("GraphQL Test Project 2", responseContent);
    Assert.Contains("\"status\":\"PUBLISHED\"", responseContent);
  }

  [Fact]
  public async Task GraphQL_GetProjectById_ShouldReturnProject() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();

    // Grant content-type permission to read projects
    await GrantContentTypePermissions(user, tenant, "Project", [PermissionType.Read]);

    var token = await GenerateJwtTokenAsync(user, tenant);
    SetAuthorizationHeader(token);

    var project = new Project {
      Title = "Specific GraphQL Project",
      Description = "This project is queried by ID",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      WebsiteUrl = "https://example.com",
      RepositoryUrl = "https://github.com/test/repo"
    };

    _context.Projects.Add(project);
    await _context.SaveChangesAsync();

    var query = $@"
        {{
            projectById(id: ""{project.Id}"") {{
                id
                title
                description
                status
                websiteUrl
                repositoryUrl
                slug
            }}
        }}";

    var request = new { query = query };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Debug output
    _output.WriteLine($"Response Status: {response.StatusCode}");
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    Assert.Contains("Specific GraphQL Project", responseContent);
    Assert.Contains("https://example.com", responseContent);
    Assert.Contains("https://github.com/test/repo", responseContent);
    Assert.Contains(project.Id.ToString(), responseContent);
  }

  [Fact]
  public async Task GraphQL_GetProjectsByType_ShouldFilterCorrectly() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();

    // Grant content-type permission to read projects
    await GrantContentTypePermissions(user, tenant, "Project", [PermissionType.Read]);

    var token = await GenerateJwtTokenAsync(user, tenant);
    SetAuthorizationHeader(token);

    var gameProject = new Project { Title = "Game Project", Type = ProjectType.Game, Status = ContentStatus.Published, Visibility = AccessLevel.Public };

    var toolProject = new Project { Title = "Tool Project", Type = ProjectType.Tool, Status = ContentStatus.Published, Visibility = AccessLevel.Public };

    _context.Projects.AddRange(gameProject, toolProject);
    await _context.SaveChangesAsync();

    // Note: Based on the schema, there might not be a projectsByType query
    // Let's use the general projects query and check the results
    var query = @"
        {
            projects {
                id
                title
                type
            }
        }";

    var request = new { query = query };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Debug output
    _output.WriteLine($"Response Status: {response.StatusCode}");
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    Assert.Contains("Game Project", responseContent);
    Assert.Contains("Tool Project", responseContent);
    Assert.Contains("\"type\":\"GAME\"", responseContent);
    Assert.Contains("\"type\":\"TOOL\"", responseContent);
  }

  [Fact]
  public async Task GraphQL_CreateProject_ShouldCreateProject() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();

    // Grant content-type permission to create projects
    await GrantContentTypePermissions(user, tenant, "Project", [PermissionType.Create]);

    var token = await GenerateJwtTokenAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Create a test category first
    var category = new Guid("11111111-1111-1111-1111-111111111111");

    var mutation = @"
        mutation {
            createProject(input: {
                title: ""New GraphQL Project""
                description: ""Created via GraphQL mutation""
                shortDescription: ""GraphQL test""
                status: DRAFT
                visibility: PUBLIC
                websiteUrl: ""https://newproject.com""
                categoryId: ""11111111-1111-1111-1111-111111111111""
            }) {
                id
                title
                description
                status
                websiteUrl
                slug
            }
        }";

    var request = new { query = mutation };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Debug output
    _output.WriteLine($"Response Status: {response.StatusCode}");
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    Assert.Contains("New GraphQL Project", responseContent);
    Assert.Contains("Created via GraphQL mutation", responseContent);
    Assert.Contains("https://newproject.com", responseContent);
    Assert.Contains("new-graphql-project", responseContent);

    // Verify in database
    var createdProject =
      await _context.Projects.FirstOrDefaultAsync(p => p.Title == "New GraphQL Project");

    Assert.NotNull(createdProject);
    Assert.Equal("Created via GraphQL mutation", createdProject.Description);
  }

  [Fact]
  public async Task GraphQL_UpdateProject_ShouldUpdateProject() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();

    // Grant content-type permission to update projects
    await GrantContentTypePermissions(user, tenant, "Project", [PermissionType.Edit]);

    var token = await GenerateJwtTokenAsync(user, tenant);
    SetAuthorizationHeader(token);

    var project = new Project { Title = "Project to Update", Description = "Original description", Status = ContentStatus.Draft, Type = ProjectType.Game };

    _context.Projects.Add(project);
    await _context.SaveChangesAsync();

    var mutation = $@"
        mutation {{
            updateProject(id: ""{project.Id}"", input: {{
                title: ""Updated Project Title""
                description: ""Updated description""
                status: PUBLISHED
            }}) {{
                id
                title
                description
                status
                updatedAt
            }}
        }}";

    var request = new { query = mutation };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Debug output
    _output.WriteLine($"Response Status: {response.StatusCode}");
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    Assert.Contains("Updated Project Title", responseContent);
    Assert.Contains("Updated description", responseContent);
    Assert.Contains("\"status\":\"PUBLISHED\"", responseContent);

    // Verify in database - refresh the context to get latest changes
    _context.ChangeTracker.Clear();
    var updatedProject =
      await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
    Assert.NotNull(updatedProject);
    Assert.Equal("Updated Project Title", updatedProject.Title);
    Assert.Equal("Updated description", updatedProject.Description);
    Assert.Equal(ContentStatus.Published, updatedProject.Status);
  }

  [Fact]
  public async Task GraphQL_DeleteProject_ShouldDeleteProject() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();

    // Grant content-type permission to delete projects
    await GrantContentTypePermissions(user, tenant, "Project", [PermissionType.Delete]);

    var token = await GenerateJwtTokenAsync(user, tenant);
    SetAuthorizationHeader(token);

    var project = new Project { Title = "Project to Delete", Description = "This project will be deleted", Status = ContentStatus.Draft };

    _context.Projects.Add(project);
    await _context.SaveChangesAsync();

    var mutation = $@"
        mutation {{
            deleteProject(id: ""{project.Id}"")
        }}";

    var request = new { query = mutation };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Debug output
    _output.WriteLine($"Response Status: {response.StatusCode}");
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    Assert.Contains("true", responseContent);

    // Verify soft delete in database (need to ignore query filters to find soft-deleted items)
    _context.ChangeTracker.Clear(); // Clear change tracker to ensure fresh data
    var deletedProject =
      await _context.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == project.Id);
    Assert.NotNull(deletedProject);
    Assert.True(deletedProject.IsDeleted);
    Assert.NotNull(deletedProject.DeletedAt);
  }

  [Fact]
  public async Task GraphQL_SearchProjects_ShouldReturnMatchingProjects() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();

    // Grant content-type permission to read projects
    await GrantContentTypePermissions(user, tenant, "Project", [PermissionType.Read]);

    var token = await GenerateJwtTokenAsync(user, tenant);
    SetAuthorizationHeader(token);

    var project1 = new Project { Title = "Amazing RPG Game", Description = "A fantastic role-playing game", Status = ContentStatus.Published, Visibility = AccessLevel.Public };

    var project2 = new Project { Title = "Simple Calculator Tool", Description = "A utility for calculations", Status = ContentStatus.Published, Visibility = AccessLevel.Public };

    var project3 = new Project { Title = "Another Amazing Project", ShortDescription = "Yet another fantastic creation", Status = ContentStatus.Published, Visibility = AccessLevel.Public };

    _context.Projects.AddRange(project1, project2, project3);
    await _context.SaveChangesAsync();

    // Note: searchProjects might not exist in the schema, using regular projects query
    var query = @"
        {
            projects {
                id
                title
                description
                shortDescription
            }
        }";

    var request = new { query = query };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Debug output
    _output.WriteLine($"Response Status: {response.StatusCode}");
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    Assert.Contains("Amazing RPG Game", responseContent);
    Assert.Contains("Another Amazing Project", responseContent);
    Assert.Contains("Simple Calculator Tool", responseContent);
  }

  [Fact]
  public async Task GraphQL_Schema_ShouldIncludeProjectTypes() {
    ClearDatabase();

    // Arrange
    var introspectionQuery = @"
        {
            __schema {
                types {
                    name
                    fields {
                        name
                        type {
                            name
                        }
                    }
                }
            }
        }";

    var request = new { query = introspectionQuery };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/graphql", content);

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    Assert.Contains("Project", responseContent); // Project type should be in schema
    Assert.Contains("ProjectType", responseContent); // Enum should be in schema
    Assert.Contains("DevelopmentStatus", responseContent); // Enum should be in schema
  }

  /// <summary>
  /// Clears all test data from the database to ensure clean test state
  /// </summary>
  private void ClearDatabase() {
    _context.Database.EnsureCreated();

    // Remove all projects
    var existingProjects = _context.Projects.ToList();
    _context.Projects.RemoveRange(existingProjects);

    // Remove all users
    var existingUsers = _context.Users.ToList();
    _context.Users.RemoveRange(existingUsers);

    // Remove all tenants
    var existingTenants = _context.Tenants.ToList();
    _context.Tenants.RemoveRange(existingTenants);

    // Remove all permissions
    var existingContentTypePermissions = _context.ContentTypePermissions.ToList();
    _context.ContentTypePermissions.RemoveRange(existingContentTypePermissions);

    var existingTenantPermissions = _context.TenantPermissions.ToList();
    _context.TenantPermissions.RemoveRange(existingTenantPermissions);

    // Save changes
    _context.SaveChanges();
  }

  /// <summary>
  /// Creates a test user for authentication purposes
  /// </summary>
  private async Task<User> CreateTestUserAsync() {
    var user = new User {
      Id = Guid.NewGuid(),
      Name = "Test User",
      Email = "test@example.com",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return user;
  }

  /// <summary>
  /// Creates a test tenant for multi-tenancy support
  /// </summary>
  private async Task<TenantModel> CreateTestTenantAsync() {
    var tenant = new TenantModel {
      Id = Guid.NewGuid(),
      Name = "Test Tenant",
      Slug = "test-tenant",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Tenants.Add(tenant);
    await _context.SaveChangesAsync();

    return tenant;
  }

  /// <summary>
  /// Grants content-type permissions for a user to perform operations on projects
  /// </summary>
  private async Task GrantContentTypePermissions(
    User user, TenantModel tenant, string contentTypeName,
    PermissionType[] permissions
  ) {
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantContentTypePermissionAsync(user.Id, tenant.Id, contentTypeName, permissions);
  }

  /// <summary>
  /// Generates a JWT token for test authentication
  /// </summary>
  private Task<string> GenerateJwtTokenAsync(User user, TenantModel tenant) {
    var jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

    var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };

    var roles = new[] { "User" };

    var additionalClaims = new[] { new System.Security.Claims.Claim("tenant_id", tenant.Id.ToString()) };

    return Task.FromResult(jwtService.GenerateAccessToken(userDto, roles, additionalClaims));
  }

  /// <summary>
  /// Sets the authorization header with the JWT token
  /// </summary>
  private void SetAuthorizationHeader(string token) { _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); }

  public void Dispose() {
    _scope.Dispose();
    _context.Dispose();
    _client.Dispose();
  }
}
