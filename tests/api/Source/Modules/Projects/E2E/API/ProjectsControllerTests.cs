using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.API.Tests.Fixtures;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Projects.Models;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using TenantModel = GameGuild.Modules.Tenants.Tenant;


namespace GameGuild.API.Tests.Modules.Projects.E2E.API;

/// <summary>
/// Integration tests for ProjectsController
/// Tests HTTP endpoints with real database and full application stack
/// </summary>
public class ProjectsControllerTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;

  private readonly HttpClient _client;

  private readonly ApplicationDbContext _context;

  private readonly IServiceScope _scope;

  private readonly ITestOutputHelper _output;

  public ProjectsControllerTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
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
  public async Task GetProjects_ShouldReturnEmptyArray_WhenNoProjects() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();

    // Grant content-type permission to read projects
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantContentTypePermissionAsync(user.Id, tenant.Id, "Project", [PermissionType.Read]);

    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token); // Act
    var response = await _client.GetAsync("/projects");

    // Debug: Check what we actually got
    var content = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Status: {response.StatusCode}");
    _output.WriteLine($"Response Content: {content}");
    _output.WriteLine(
      $"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(";", h.Value)}"))}"
    );

    // Assert
    Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}. Content: {content}");
    var projects = JsonSerializer.Deserialize<Project[]>(
      content,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(projects);
    Assert.Empty(projects);
  }

  [Fact]
  public async Task GetProjects_ShouldReturnProjects_WhenProjectsExist() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();
    await GrantContentTypePermissions(user, tenant, "Project", new[] { PermissionType.Read });
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var project1 = new Project {
      Title = "Test Project 1",
      Description = "Description 1",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Type = ProjectType.Game,
      DevelopmentStatus = DevelopmentStatus.InDevelopment,
      CreatedById = user.Id, // Associate with the test user
      TenantId = tenant.Id,
    };

    var project2 = new Project {
      Title = "Test Project 2",
      Description = "Description 2",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Type = ProjectType.Tool,
      DevelopmentStatus = DevelopmentStatus.Released,
      CreatedById = user.Id, // Associate with the test user
      TenantId = tenant.Id,
    };

    _context.Projects.AddRange(project1, project2);
    await _context.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync("/projects");

    // Assert
    Assert.True(response.IsSuccessStatusCode);
    var content = await response.Content.ReadAsStringAsync();
    var projects = JsonSerializer.Deserialize<Project[]>(
      content,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(projects);
    Assert.Equal(2, projects.Length);
    Assert.Contains(projects, p => p.Title == "Test Project 1");
    Assert.Contains(projects, p => p.Title == "Test Project 2");
  }

  [Fact]
  public async Task CreateProject_ShouldCreateProject_WithValidData() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token); // Grant the user permission to create projects
    await GrantContentTypePermissions(user, tenant, "Project", new[] { PermissionType.Create });

    // Create a proper Project object instead of an anonymous object
    var project = new Project {
      Title = "New Test Project",
      Description = "This is a test project",
      ShortDescription = "Test project",
      Status = ContentStatus.Draft,
      Visibility = AccessLevel.Public,
      Type = ProjectType.Game,
      DevelopmentStatus = DevelopmentStatus.Planning,
      WebsiteUrl = "https://example.com",
      RepositoryUrl = "https://github.com/test/repo",
      CreatedById = user.Id,
      TenantId = tenant.Id,
    };

    var json = JsonSerializer.Serialize(project);
    _output.WriteLine($"Request payload: {json}");
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/projects", content);

    // Debug the response
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Status: {response.StatusCode}");
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(
      response.IsSuccessStatusCode,
      $"Expected success status code but got {response.StatusCode}. Content: {responseContent}"
    );

    var createdProject = JsonSerializer.Deserialize<Project>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(createdProject);
    Assert.Equal("New Test Project", createdProject.Title);
    Assert.Equal("This is a test project", createdProject.Description);
    Assert.NotEqual(Guid.Empty, createdProject.Id);

    // Verify in database
    var dbProject =
      await _context.Projects.FirstOrDefaultAsync(p => p.Id == createdProject.Id);
    Assert.NotNull(dbProject);
    Assert.Equal("New Test Project", dbProject.Title);
  }

  [Fact]
  public async Task CreateProject_ShouldReturnBadRequest_WithInvalidData() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();
    await GrantContentTypePermissions(user, tenant, "Project", new[] { PermissionType.Create });
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    // Missing required Title
    var projectData = new { Description = "This is a test project without title" };

    var json = JsonSerializer.Serialize(projectData);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/projects", content);

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetProject_ShouldReturnProject_WhenExists() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();
    await GrantContentTypePermissions(user, tenant, "Project", new[] { PermissionType.Read });
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var project = new Project {
      Title = "Specific Test Project",
      Description = "This is a specific test project",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = user.Id,
      TenantId = tenant.Id,
    };

    _context.Projects.Add(project);
    await _context.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync($"/projects/{project.Id}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var returnedProject = JsonSerializer.Deserialize<Project>(
      content,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(returnedProject);
    Assert.Equal("Specific Test Project", returnedProject.Title);
    Assert.Equal(project.Id, returnedProject.Id);
  }

  [Fact]
  public async Task GetProject_ShouldReturnNotFound_WhenNotExists() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();
    await GrantContentTypePermissions(user, tenant, "Project", new[] { PermissionType.Read });
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await _client.GetAsync($"/projects/{nonExistentId}");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetProjectBySlug_ShouldReturnProject_WhenExists() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();
    // No authentication needed since endpoint is [Public]
    // Create test user for project ownership
    var user = await CreateTestUserAsync();

    var project = new Project {
      Title = "Slug Test Project",
      Description = "This project tests slug functionality",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = user.Id,
    };

    // Manually set the slug using the Project's GenerateSlug method
    project.Slug = Project.GenerateSlug(project.Title);

    _context.Projects.Add(project);
    await _context.SaveChangesAsync(); // Debug: Verify the project was saved
    var savedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
    _output.WriteLine($"Project saved in DB: {savedProject != null}");

    if (savedProject != null) _output.WriteLine($"Saved project: ID={savedProject.Id}, Title={savedProject.Title}, Slug={savedProject.Slug}");

    var allProjects = await _context.Projects.ToListAsync();
    _output.WriteLine($"Total projects in DB: {allProjects.Count}");

    var expectedSlug = project.Slug; // This should be "slug-test-project"
    _output.WriteLine($"Expected slug: {expectedSlug}");

    // Act
    var response = await _client.GetAsync($"/projects/slug/{expectedSlug}");

    // Debug the response
    var responseContent = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Response Status: {response.StatusCode}");
    _output.WriteLine($"Response Content: {responseContent}");

    // Assert
    Assert.True(
      response.IsSuccessStatusCode,
      $"Expected success but got {response.StatusCode}. Content: {responseContent}"
    );

    var returnedProject = JsonSerializer.Deserialize<Project>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    Assert.NotNull(returnedProject);
    Assert.Equal("Slug Test Project", returnedProject.Title);
    Assert.Equal(project.Id, returnedProject.Id);
  }

  [Fact]
  public async Task GetProjectBySlug_ShouldReturnNotFound_WhenNotExists() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Act
    var response = await _client.GetAsync("/projects/slug/non-existent-slug");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetProjectsByCategory_ShouldReturnProjectsInCategory() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();
    await GrantContentTypePermissions(user, tenant, "Project", new[] { PermissionType.Read });
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var categoryId = Guid.NewGuid();

    var project1 = new Project {
      Title = "Category Project 1",
      CategoryId = categoryId,
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = user.Id,
      TenantId = tenant.Id,
    };

    var project2 = new Project {
      Title = "Category Project 2",
      CategoryId = categoryId,
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = user.Id,
      TenantId = tenant.Id,
    };

    var project3 = new Project {
      Title = "Other Category Project",
      CategoryId = Guid.NewGuid(), // Different category
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = user.Id,
      TenantId = tenant.Id,
    };

    _context.Projects.AddRange(project1, project2, project3);
    await _context.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync($"/projects/category/{categoryId}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var projects = JsonSerializer.Deserialize<Project[]>(
      content,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(projects);
    Assert.Equal(2, projects.Length);
    Assert.Contains(projects, p => p.Title == "Category Project 1");
    Assert.Contains(projects, p => p.Title == "Category Project 2");
    Assert.DoesNotContain(projects, p => p.Title == "Other Category Project");
  }

  [Fact]
  public async Task GetProjectsByStatus_ShouldReturnProjectsWithStatus() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user and tenant for authentication
    var user = await CreateTestUserAsync();
    var tenant = await CreateTestTenantAsync();
    await GrantContentTypePermissions(user, tenant, "Project", new[] { PermissionType.Read });
    var token = await CreateJwtTokenForUserAsync(user, tenant);
    SetAuthorizationHeader(token);

    var publishedProject = new Project {
      Title = "Published Project",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = user.Id,
      TenantId = tenant.Id,
    };

    var draftProject = new Project {
      Title = "Draft Project",
      Status = ContentStatus.Draft,
      Visibility = AccessLevel.Public,
      CreatedById = user.Id,
      TenantId = tenant.Id,
    };

    _context.Projects.AddRange(publishedProject, draftProject);
    await _context.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync($"/projects/status/{ContentStatus.Published}");

    // Assert
    Assert.True(response.IsSuccessStatusCode);

    var content = await response.Content.ReadAsStringAsync();
    var projects = JsonSerializer.Deserialize<Project[]>(
      content,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(projects);
    Assert.Single(projects);
    Assert.Equal("Published Project", projects.First().Title);
  }

  [Fact]
  public async Task GetPublicProjects_ShouldReturnOnlyPublicPublishedProjects() {
    // Arrange - Clear database to ensure clean state
    ClearDatabase();

    // Create test user for project ownership
    var user = await CreateTestUserAsync();

    // Use the default test tenant that MockTenantContextService provides
    var testTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var tenant = new TenantModel { Id = testTenantId, Name = "Test Tenant", Slug = "test-tenant", IsActive = true };

    _context.Tenants.Add(tenant);

    var publicProject = new Project {
      Title = "Public Project",
      Description = "A public project",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Tenant = null, // Make it global - accessible across all tenants
      CreatedById = user.Id,
    };

    var privateProject = new Project {
      Title = "Private Project",
      Description = "A private project",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Private,
      Tenant = null, // Make it global - accessible across all tenants
      CreatedById = user.Id,
    };

    var draftProject = new Project {
      Title = "Draft Public Project",
      Description = "A draft project",
      Status = ContentStatus.Draft,
      Visibility = AccessLevel.Public,
      Tenant = null, // Make it global - accessible across all tenants
      CreatedById = user.Id,
    };

    _context.Projects.AddRange(publicProject, privateProject, draftProject);
    await _context.SaveChangesAsync();

    // DEBUG: Check if projects were created successfully
    var savedProjects = await _context.Projects.ToListAsync();
    Console.WriteLine($"DEBUG TEST: Projects in database after save: {savedProjects.Count}");

    foreach (var p in savedProjects) {
      Console.WriteLine(
        $"DEBUG TEST: Project in DB: {p.Title}, Status: {p.Status}, Visibility: {p.Visibility}, TenantId: {(p.Tenant?.Id.ToString() ?? "null")}, IsDeleted: {p.DeletedAt != null}"
      );
    }

    // Act
    var response = await _client.GetAsync("/projects/public");

    // Debug: Check what we actually got
    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Response Status: {response.StatusCode}");
    Console.WriteLine($"Response Content: {content}");

    // Assert
    Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}. Content: {content}");

    var projects = JsonSerializer.Deserialize<Project[]>(
      content,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(projects);
    Assert.Single(projects);
    Assert.Equal("Public Project", projects.First().Title);
  }

  #region Helper Methods

  private async Task<User> CreateTestUserAsync() {
    var user = new User { Id = Guid.NewGuid(), Name = "Test User", Email = "test@example.com", IsActive = true };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return user;
  }

  private async Task<TenantModel> CreateTestTenantAsync() {
    var tenant = new TenantModel { Id = Guid.NewGuid(), Name = "Test Tenant", Description = "Test tenant for integration tests", IsActive = true };

    _context.Tenants.Add(tenant);
    await _context.SaveChangesAsync();

    return tenant;
  }

  private Task<string> CreateJwtTokenForUserAsync(User user, TenantModel tenant) {
    var jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

    var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };

    var roles = new[] { "User" };

    var additionalClaims = new[] { new System.Security.Claims.Claim("tenant_id", tenant.Id.ToString()) };

    return Task.FromResult(jwtService.GenerateAccessToken(userDto, roles, additionalClaims));
  }

  private async Task GrantProjectPermissions(
    User user, TenantModel tenant,
    Project project, PermissionType[] permissions
  ) {
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService
      .GrantResourcePermissionAsync<ProjectPermission,
        Project>(user.Id, tenant.Id, project.Id, permissions);
  }

  private async Task GrantContentTypePermissions(
    User user, TenantModel tenant, string contentTypeName,
    PermissionType[] permissions
  ) {
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantContentTypePermissionAsync(user.Id, tenant.Id, contentTypeName, permissions);
  }

  private void SetAuthorizationHeader(string token) { _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); }

  private void ClearAuthorizationHeader() { _client.DefaultRequestHeaders.Authorization = null; }

  #endregion

  /// <summary>
  /// Clear the in-memory database to ensure clean state between tests
  /// </summary>
  private void ClearDatabase() {
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

  public void Dispose() {
    _scope.Dispose();
    _context.Dispose();
    _client.Dispose();
  }
}
