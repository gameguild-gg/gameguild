using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using GameGuild.Projects.UnitTests.Infrastructure;

namespace GameGuild.Projects.UnitTests.Controllers;

public class ProjectsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly TestDataBuilder _testDataBuilder;

    public ProjectsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _testDataBuilder = new TestDataBuilder();
    }

    [Fact]
    public async Task GetProjects_Should_Return_Ok_With_Projects()
    {
        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("type", "Game")]
    [InlineData("status", "Published")]
    [InlineData("visibility", "Public")]
    public async Task GetProjects_Should_Accept_Query_Parameters(string paramName, string paramValue)
    {
        // Act
        var response = await _client.GetAsync($"/api/projects?{paramName}={paramValue}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProjects_Should_Accept_Pagination_Parameters()
    {
        // Act
        var response = await _client.GetAsync("/api/projects?skip=0&take=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProjects_Should_Accept_Search_Parameter()
    {
        // Act
        var response = await _client.GetAsync("/api/projects?searchTerm=game");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProjectById_Should_Return_NotFound_For_Invalid_Id()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/projects/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProjectBySlug_Should_Return_NotFound_For_Invalid_Slug()
    {
        // Act
        var response = await _client.GetAsync("/api/projects/slug/non-existent-slug");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProject_Should_Require_Authentication()
    {
        // Arrange
        var createRequest = new
        {
            Title = "Test Project",
            Description = "Test description",
            Type = "Game"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProject_Should_Require_Authentication()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var updateRequest = new
        {
            Title = "Updated Project"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/projects/{projectId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProject_Should_Require_Authentication()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/projects/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetProjectBySlug_Should_Handle_Invalid_Slugs(string slug)
    {
        // Act
        var response = await _client.GetAsync($"/api/projects/slug/{slug}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjects_Should_Handle_Invalid_Pagination_Parameters()
    {
        // Act
        var response = await _client.GetAsync("/api/projects?skip=-1&take=0");

        // Assert
        // Should either return OK with adjusted parameters or BadRequest
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjects_Should_Handle_Large_Take_Values()
    {
        // Act
        var response = await _client.GetAsync("/api/projects?take=1000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}