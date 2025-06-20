using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using GameGuild.Data;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Tests.Integration.Project.GraphQL;

/// <summary>
/// Integration tests for Project GraphQL operations
/// Tests GraphQL queries and mutations with real database
/// </summary>
public class ProjectGraphQLTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _context;

    public ProjectGraphQLTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                ServiceDescriptor? descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"GraphQLTestDatabase_{Guid.NewGuid()}");
                });

                // Set environment variable for in-memory database
                Environment.SetEnvironmentVariable("USE_IN_MEMORY_DB", "true");
            });
        });

        _client = _factory.CreateClient();

        // Get database context for test setup
        IServiceScope scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task GraphQL_GetProjects_ShouldReturnProjects()
    {
        // Arrange
        var project1 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "GraphQL Test Project 1",
            Description = "First test project for GraphQL",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public,
            Type = ProjectType.Game
        };

        var project2 = new GameGuild.Modules.Project.Models.Project
        {
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

        var request = new
        {
            query = query
        };

        string json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("GraphQL Test Project 1", responseContent);
        Assert.Contains("GraphQL Test Project 2", responseContent);
        Assert.Contains("\"status\":\"Published\"", responseContent);
    }

    [Fact]
    public async Task GraphQL_GetProjectById_ShouldReturnProject()
    {
        // Arrange
        var project = new GameGuild.Modules.Project.Models.Project
        {
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
            project(id: ""{project.Id}"") {{
                id
                title
                description
                status
                websiteUrl
                repositoryUrl
                slug
            }}
        }}";

        var request = new
        {
            query = query
        };

        string json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Specific GraphQL Project", responseContent);
        Assert.Contains("https://example.com", responseContent);
        Assert.Contains("https://github.com/test/repo", responseContent);
        Assert.Contains(project.Id.ToString(), responseContent);
    }

    [Fact]
    public async Task GraphQL_GetProjectsByType_ShouldFilterCorrectly()
    {
        // Arrange
        var gameProject = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Game Project",
            Type = ProjectType.Game,
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        var toolProject = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Tool Project",
            Type = ProjectType.Tool,
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        _context.Projects.AddRange(gameProject, toolProject);
        await _context.SaveChangesAsync();

        var query = @"
        {
            projectsByType(type: GAME) {
                id
                title
                type
            }
        }";

        var request = new
        {
            query = query
        };

        string json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Game Project", responseContent);
        Assert.DoesNotContain("Tool Project", responseContent);
        Assert.Contains("\"type\":\"Game\"", responseContent);
    }

    [Fact]
    public async Task GraphQL_CreateProject_ShouldCreateProject()
    {
        // Arrange
        var mutation = @"
        mutation {
            createProject(input: {
                title: ""New GraphQL Project""
                description: ""Created via GraphQL mutation""
                shortDescription: ""GraphQL test""
                status: DRAFT
                visibility: PUBLIC
                type: GAME
                developmentStatus: PLANNING
                websiteUrl: ""https://newproject.com""
            }) {
                id
                title
                description
                status
                type
                websiteUrl
                slug
            }
        }";

        var request = new
        {
            query = mutation
        };

        string json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("New GraphQL Project", responseContent);
        Assert.Contains("Created via GraphQL mutation", responseContent);
        Assert.Contains("https://newproject.com", responseContent);
        Assert.Contains("new-graphql-project", responseContent);

        // Verify in database
        GameGuild.Modules.Project.Models.Project? createdProject = await _context.Projects
            .FirstOrDefaultAsync(p => p.Title == "New GraphQL Project");
        
        Assert.NotNull(createdProject);
        Assert.Equal("Created via GraphQL mutation", createdProject.Description);
        Assert.Equal(ProjectType.Game, createdProject.Type);
    }

    [Fact]
    public async Task GraphQL_UpdateProject_ShouldUpdateProject()
    {
        // Arrange
        var project = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Project to Update",
            Description = "Original description",
            Status = ContentStatus.Draft,
            Type = ProjectType.Game
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var mutation = $@"
        mutation {{
            updateProject(input: {{
                id: ""{project.Id}""
                title: ""Updated Project Title""
                description: ""Updated description""
                status: PUBLISHED
                type: TOOL
            }}) {{
                id
                title
                description
                status
                type
                updatedAt
            }}
        }}";

        var request = new
        {
            query = mutation
        };

        string json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Updated Project Title", responseContent);
        Assert.Contains("Updated description", responseContent);
        Assert.Contains("\"status\":\"Published\"", responseContent);
        Assert.Contains("\"type\":\"Tool\"", responseContent);

        // Verify in database
        GameGuild.Modules.Project.Models.Project? updatedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        Assert.NotNull(updatedProject);
        Assert.Equal("Updated Project Title", updatedProject.Title);
        Assert.Equal("Updated description", updatedProject.Description);
        Assert.Equal(ContentStatus.Published, updatedProject.Status);
        Assert.Equal(ProjectType.Tool, updatedProject.Type);
    }

    [Fact]
    public async Task GraphQL_DeleteProject_ShouldDeleteProject()
    {
        // Arrange
        var project = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Project to Delete",
            Description = "This project will be deleted",
            Status = ContentStatus.Draft
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var mutation = $@"
        mutation {{
            deleteProject(id: ""{project.Id}"") {{
                success
                message
            }}
        }}";

        var request = new
        {
            query = mutation
        };

        string json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", responseContent);

        // Verify soft delete in database
        GameGuild.Modules.Project.Models.Project? deletedProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        Assert.NotNull(deletedProject);
        Assert.True(deletedProject.IsDeleted);
        Assert.NotNull(deletedProject.DeletedAt);
    }

    [Fact]
    public async Task GraphQL_SearchProjects_ShouldReturnMatchingProjects()
    {
        // Arrange
        var project1 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Amazing RPG Game",
            Description = "A fantastic role-playing game",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        var project2 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Simple Calculator Tool",
            Description = "A utility for calculations",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        var project3 = new GameGuild.Modules.Project.Models.Project
        {
            Title = "Another Amazing Project",
            ShortDescription = "Yet another fantastic creation",
            Status = ContentStatus.Published,
            Visibility = AccessLevel.Public
        };

        _context.Projects.AddRange(project1, project2, project3);
        await _context.SaveChangesAsync();

        var query = @"
        {
            searchProjects(searchTerm: ""amazing"") {
                id
                title
                description
                shortDescription
            }
        }";

        var request = new
        {
            query = query
        };

        string json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Amazing RPG Game", responseContent);
        Assert.Contains("Another Amazing Project", responseContent);
        Assert.DoesNotContain("Simple Calculator Tool", responseContent);
    }

    [Fact]
    public async Task GraphQL_Schema_ShouldIncludeProjectTypes()
    {
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

        var request = new
        {
            query = introspectionQuery
        };

        string json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client.PostAsync("/graphql", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Project", responseContent); // Project type should be in schema
        Assert.Contains("ProjectType", responseContent); // Enum should be in schema
        Assert.Contains("DevelopmentStatus", responseContent); // Enum should be in schema
    }

    public void Dispose()
    {
        _context.Dispose();
        _client.Dispose();
    }
}
