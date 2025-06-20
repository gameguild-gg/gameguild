using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using GameGuild.Data;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Project.Models;
using GameGuild.Tests.Fixtures;

namespace GameGuild.Tests.Integration.Project.Controllers;

/// <summary>
/// Integration tests for ProjectsController
/// Tests HTTP endpoints with real database and full application stack
/// </summary>
public class ProjectsControllerTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _context;

    public ProjectsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Get database context for test setup
        using IServiceScope scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task GetProjects_ShouldReturnEmptyArray_WhenNoProjects()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/projects");

        // Debug: Check what we actually got
        string content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}. Content: {content}");
        var projects = JsonSerializer.Deserialize<GameGuild.Modules.Project.Models.Project[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task GetProjects_ShouldReturnProjects_WhenProjectsExist()
    {
        // Arrange
        var project1 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Test Project 1",
            Description = "Description 1",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public,
            Type = ProjectType.Game,
            DevelopmentStatus = DevelopmentStatus.InDevelopment
        };

        var project2 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Test Project 2",
            Description = "Description 2",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public,
            Type = ProjectType.Tool,
            DevelopmentStatus = DevelopmentStatus.Released
        };

        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/projects");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        string content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<GameGuild.Modules.Project.Models.Project[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(projects);
        Assert.Equal(2, projects.Length);
        Assert.Contains(projects, p => p.Title == "Test Project 1");
        Assert.Contains(projects, p => p.Title == "Test Project 2");
    }

    [Fact]
    public async Task CreateProject_ShouldCreateProject_WithValidData()
    {
        // Arrange
        var projectData = new
        {
            Title = "New Test Project",
            Description = "This is a test project",
            ShortDescription = "Test project",
            Status = "Draft",
            Visibility = "Public",
            Type = "Game",
            DevelopmentStatus = "Planning",
            WebsiteUrl = "https://example.com",
            RepositoryUrl = "https://github.com/test/repo"
        };

        string json = JsonSerializer.Serialize(projectData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/projects", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        var createdProject = JsonSerializer.Deserialize<GameGuild.Modules.Project.Models.Project>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(createdProject);
        Assert.Equal("New Test Project", createdProject.Title);
        Assert.Equal("This is a test project", createdProject.Description);
        Assert.NotEqual(Guid.Empty, createdProject.Id);

        // Verify in database
        GameGuild.Modules.Project.Models.Project? dbProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == createdProject.Id);
        Assert.NotNull(dbProject);
        Assert.Equal("New Test Project", dbProject.Title);
    }

    [Fact]
    public async Task CreateProject_ShouldReturnBadRequest_WithInvalidData()
    {
        // Arrange - Missing required Title
        var projectData = new
        {
            Description = "This is a test project without title"
        };

        string json = JsonSerializer.Serialize(projectData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/projects", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_ShouldReturnProject_WhenExists()
    {
        // Arrange
        var project = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Specific Test Project",
            Description = "This is a specific test project",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/projects/{project.Id}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string content = await response.Content.ReadAsStringAsync();
        var returnedProject = JsonSerializer.Deserialize<GameGuild.Modules.Project.Models.Project>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(returnedProject);
        Assert.Equal("Specific Test Project", returnedProject.Title);
        Assert.Equal(project.Id, returnedProject.Id);
    }

    [Fact]
    public async Task GetProject_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/projects/{nonExistentId}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectBySlug_ShouldReturnProject_WhenExists()
    {
        // Arrange
        var project = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Slug Test Project",
            Description = "This project tests slug functionality",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        string expectedSlug = project.Slug; // This should be "slug-test-project"

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/projects/slug/{expectedSlug}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string content = await response.Content.ReadAsStringAsync();
        var returnedProject = JsonSerializer.Deserialize<GameGuild.Modules.Project.Models.Project>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(returnedProject);
        Assert.Equal("Slug Test Project", returnedProject.Title);
        Assert.Equal(project.Id, returnedProject.Id);
    }

    [Fact]
    public async Task GetProjectBySlug_ShouldReturnNotFound_WhenNotExists()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/projects/slug/non-existent-slug");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectsByCategory_ShouldReturnProjectsInCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        
        var project1 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Category Project 1",
            CategoryId = categoryId,
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        var project2 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Category Project 2",
            CategoryId = categoryId,
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        var project3 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Other Category Project",
            CategoryId = Guid.NewGuid(), // Different category
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        _context.Projects.AddRange(project1, project2, project3);
        await _context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/projects/category/{categoryId}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<GameGuild.Modules.Project.Models.Project[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(projects);
        Assert.Equal(2, projects.Length);
        Assert.Contains(projects, p => p.Title == "Category Project 1");
        Assert.Contains(projects, p => p.Title == "Category Project 2");
        Assert.DoesNotContain(projects, p => p.Title == "Other Category Project");
    }

    [Fact]
    public async Task GetProjectsByStatus_ShouldReturnProjectsWithStatus()
    {
        // Arrange
        var publishedProject = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Published Project",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        var draftProject = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Draft Project",
            Status = ContentStatus.Draft,
            Visibility = AccessLevel.Public
        };

        _context.Projects.AddRange(publishedProject, draftProject);
        await _context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/projects/status/{ContentStatus.Published}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<GameGuild.Modules.Project.Models.Project[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Equal("Published Project", projects.First().Title);
    }

    [Fact]
    public async Task GetPublicProjects_ShouldReturnOnlyPublicPublishedProjects()
    {
        // Arrange
        var publicProject = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Public Project",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        var privateProject = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Private Project",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Private
        };

        var draftProject = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Draft Public Project",
            Status = ContentStatus.Draft,
            Visibility = AccessLevel.Public
        };

        _context.Projects.AddRange(publicProject, privateProject, draftProject);
        await _context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/projects/public");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<GameGuild.Modules.Project.Models.Project[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Equal("Public Project", projects.First().Title);
    }

    public void Dispose()
    {
        _context.Dispose();
        _client.Dispose();
    }
}
