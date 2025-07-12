using System.Text;
using System.Text.Json;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Projects;
using GameGuild.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using ProjectType = GameGuild.Common.ProjectType;


namespace GameGuild.Tests.Modules.Projects.E2E.API;

/// <summary>
/// Integration tests for ProjectsController
/// Tests HTTP endpoints with real database and full application stack
/// </summary>
public class ProjectsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable {
  private readonly WebApplicationFactory<Program> _factory;
  private HttpClient? _client;
  private readonly ApplicationDbContext _context;
  private readonly IServiceScope _scope;
  private readonly ITestOutputHelper _output;
  private Guid _tenantId;
  private Guid _userId;

  public ProjectsControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output) {
    // Use a unique database name for this test class to avoid interference
    var uniqueDbName = $"ProjectsControllerTests_{Guid.NewGuid()}";
    _factory = IntegrationTestHelper.GetTestFactory(uniqueDbName);
    _output = output;
    _scope = _factory.Services.CreateScope();

    // Get database context for test setup
    _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
  }

  private async Task<HttpClient> GetAuthenticatedClientAsync() {
    // Create authenticated test user with permissions
    var (client, userId, tenantId) = await IntegrationTestHelper.CreateAuthenticatedTestUserAsync(_factory);
    
    // Store the IDs for use in test assertions
    _userId = userId;
    _tenantId = tenantId;
    
    return client;
  }

  [Fact]
  public async Task GetProjects_ShouldReturnEmptyArray_WhenNoProjects() {
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    // Grant content-type permission to read projects
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantContentTypePermissionAsync(_userId, _tenantId, "Project", [PermissionType.Read]);

    // Act
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
    // Arrange
    _client = await GetAuthenticatedClientAsync();
    await GrantContentTypePermissions(_userId, _tenantId, "Project", [PermissionType.Read]);

    var project1 = new Project {
      Title = "Test Project 1",
      Description = "Description 1",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Type = ProjectType.Game,
      DevelopmentStatus = DevelopmentStatus.InDevelopment,
      CreatedById = _userId, // Associate with the test user
      TenantId = _tenantId
    };

    var project2 = new Project {
      Title = "Test Project 2",
      Description = "Description 2",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Type = ProjectType.Tool,
      DevelopmentStatus = DevelopmentStatus.Released,
      CreatedById = _userId, // Associate with the test user
      TenantId = _tenantId
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
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    // Grant the user permission to create projects
    await GrantContentTypePermissions(_userId, _tenantId, "Project", [PermissionType.Create]);

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
      CreatedById = _userId,
      TenantId = _tenantId
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
    // Arrange
    _client = await GetAuthenticatedClientAsync();

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
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    var project = new Project {
      Title = "Specific Test Project",
      Description = "This is a specific test project",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = _userId,
      TenantId = _tenantId
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
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await _client.GetAsync($"/projects/{nonExistentId}");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetProjectBySlug_ShouldReturnProject_WhenExists() {
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    var project = new Project {
      Title = "Slug Test Project",
      Description = "This project tests slug functionality",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = _userId,
      TenantId = _tenantId
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
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    // Act
    var response = await _client.GetAsync("/projects/slug/non-existent-slug");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetProjectsByCategory_ShouldReturnProjectsInCategory() {
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    var categoryId = Guid.NewGuid();

    var project1 = new Project {
      Title = "Category Project 1",
      CategoryId = categoryId,
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = _userId,
      TenantId = _tenantId
    };

    var project2 = new Project {
      Title = "Category Project 2",
      CategoryId = categoryId,
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = _userId,
      TenantId = _tenantId
    };

    var project3 = new Project {
      Title = "Other Category Project",
      CategoryId = Guid.NewGuid(), // Different category
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = _userId,
      TenantId = _tenantId
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
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    var publishedProject = new Project {
      Title = "Published Project",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      CreatedById = _userId,
      TenantId = _tenantId
    };

    var draftProject = new Project {
      Title = "Draft Project",
      Status = ContentStatus.Draft,
      Visibility = AccessLevel.Public,
      CreatedById = _userId,
      TenantId = _tenantId
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
    // Arrange
    _client = await GetAuthenticatedClientAsync();

    var publicProject = new Project {
      Title = "Public Project",
      Description = "A public project",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Public,
      Tenant = null, // Make it global - accessible across all tenants
      CreatedById = _userId
    };

    var privateProject = new Project {
      Title = "Private Project",
      Description = "A private project",
      Status = ContentStatus.Published,
      Visibility = AccessLevel.Private,
      Tenant = null, // Make it global - accessible across all tenants
      CreatedById = _userId
    };

    var draftProject = new Project {
      Title = "Draft Public Project",
      Description = "A draft project",
      Status = ContentStatus.Draft,
      Visibility = AccessLevel.Public,
      Tenant = null, // Make it global - accessible across all tenants
      CreatedById = _userId
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

  private async Task GrantProjectPermissions(
    Guid userId, Guid tenantId,
    Project project, PermissionType[] permissions
  ) {
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService
      .GrantResourcePermissionAsync<ProjectPermission,
        Project>(userId, tenantId, project.Id, permissions);
  }

  private async Task GrantContentTypePermissions(
    Guid userId, Guid tenantId, string contentTypeName,
    PermissionType[] permissions
  ) {
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantContentTypePermissionAsync(userId, tenantId, contentTypeName, permissions);
  }

  #endregion

  public void Dispose() {
    _scope.Dispose();
    _context.Dispose();
    _client?.Dispose();
    _factory.Dispose();
  }
}
