using System.Text.Json;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Tenants;
using GameGuild.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;


namespace GameGuild.Tests.Modules.Programs.Integration.Controllers;

/// <summary>
/// Integration tests for ProgramController focusing on slug-based operations
/// Tests HTTP endpoints with real database and full application stack
/// </summary>
public class ProgramControllerSlugTests : IDisposable
{
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<GameGuild.Program> _factory;
    private readonly ApplicationDbContext _context;
    private readonly IServiceScope _scope;
    private readonly ITestOutputHelper _output;

    public ProgramControllerSlugTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Use the IntegrationTestHelper to get the test factory
        var uniqueDbName = $"ProgramControllerSlugTests_{Guid.NewGuid()}";
        _factory = IntegrationTestHelper.GetTestFactory(uniqueDbName);
        _scope = _factory.Services.CreateScope();

        // Get database context for test setup
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Clear any existing data to ensure clean test state
        ClearDatabase();
    }

    [Fact]
    public async Task GetProgramBySlug_WithValidSlug_ReturnsProgram()
    {
        // Arrange - use the same test factory as the test setup
        var (authenticatedClient, userId, tenantId) = await IntegrationTestHelper.CreateAuthenticatedTestUserAsync(_factory);
        var program = await CreateTestProgramAsync(tenantId, "test-program-slug");

        // Act
        var response = await authenticatedClient.GetAsync($"/api/program/slug/{program.Slug}");

        // Assert
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var returnedProgram = JsonSerializer.Deserialize<GameGuild.Modules.Programs.Models.Program>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(returnedProgram);
        Assert.Equal(program.Slug, returnedProgram.Slug);
        Assert.Equal(program.Title, returnedProgram.Title);
        Assert.Equal(program.Description, returnedProgram.Description);
    }

    [Fact]
    public async Task GetProgramBySlug_WithInvalidSlug_ReturnsNotFound()
    {
        // Arrange
        var (authenticatedClient, userId, tenantId) = await IntegrationTestHelper.CreateAuthenticatedTestUserAsync(_factory);

        // Act
        var response = await authenticatedClient.GetAsync("/api/program/slug/non-existent-slug");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProgramBySlug_WithPublishedProgram_ReturnsSuccessWithoutAuth()
    {
        // Arrange
        var (authenticatedClient, userId, tenantId) = await IntegrationTestHelper.CreateAuthenticatedTestUserAsync(_factory);
        var program = await CreateTestProgramAsync(tenantId, "public-program-slug",
            status: ContentStatus.Published, visibility: AccessLevel.Public);

        // Act - Create unauthenticated client for this test
        var unauthenticatedClient = _factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync($"/api/program/slug/{program.Slug}");

        // Assert
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var returnedProgram = JsonSerializer.Deserialize<GameGuild.Modules.Programs.Models.Program>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(returnedProgram);
        Assert.Equal(program.Slug, returnedProgram.Slug);
    }

    [Fact]
    public async Task GetProgramBySlug_WithDraftProgram_RequiresAuthentication()
    {
        // Arrange
        var (authenticatedClient, userId, tenantId) = await IntegrationTestHelper.CreateAuthenticatedTestUserAsync(_factory);
        var program = await CreateTestProgramAsync(tenantId, "draft-program-slug",
            status: ContentStatus.Draft, visibility: AccessLevel.Private);

        // Act - Create unauthenticated client for this test
        var unauthenticatedClient = _factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync($"/api/program/slug/{program.Slug}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPublishedPrograms_ReturnsOnlyPublishedPrograms()
    {
        // Arrange
        var (authenticatedClient, userId, tenantId) = await IntegrationTestHelper.CreateAuthenticatedTestUserAsync(_factory);

        // Create multiple programs with different statuses
        var publishedProgram1 = await CreateTestProgramAsync(tenantId, "published-1",
            status: ContentStatus.Published, visibility: AccessLevel.Public);
        var publishedProgram2 = await CreateTestProgramAsync(tenantId, "published-2",
            status: ContentStatus.Published, visibility: AccessLevel.Public);
        var draftProgram = await CreateTestProgramAsync(tenantId, "draft-1",
            status: ContentStatus.Draft, visibility: AccessLevel.Private);

        // Act - This endpoint might be public, but use unauthenticated client to test
        var unauthenticatedClient = _factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/program/published");

        // Assert
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var programs = JsonSerializer.Deserialize<List<GameGuild.Modules.Programs.Models.Program>>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(programs);
        Assert.Equal(2, programs.Count);
        Assert.Contains(programs, p => p.Slug == publishedProgram1.Slug);
        Assert.Contains(programs, p => p.Slug == publishedProgram2.Slug);
        Assert.DoesNotContain(programs, p => p.Slug == draftProgram.Slug);
    }

    [Fact]
    public async Task GetProgramBySlug_WithSpecialCharactersInSlug_HandlesCorrectly()
    {
        // Arrange - use the same test factory as the test setup
        var (authenticatedClient, userId, tenantId) = await IntegrationTestHelper.CreateAuthenticatedTestUserAsync(_factory);
        var program = await CreateTestProgramAsync(tenantId, "unity-3d-game-development-2024");

        // Act
        var response = await authenticatedClient.GetAsync($"/api/program/slug/{program.Slug}");

        // Assert
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var returnedProgram = JsonSerializer.Deserialize<GameGuild.Modules.Programs.Models.Program>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(returnedProgram);
        Assert.Equal(program.Slug, returnedProgram.Slug);
    }

    #region Helper Methods

    private void ClearDatabase()
    {
        try
        {
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning: Could not clear database: {ex.Message}");
        }
    }

    private async Task<GameGuild.Modules.Programs.Models.Program> CreateTestProgramAsync(Guid tenantId, string slug,
        ContentStatus status = ContentStatus.Draft,
        AccessLevel visibility = AccessLevel.Private)
    {
        // First, create or find the tenant
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Id = tenantId,
                Name = "Test Tenant",
                Description = "Test tenant for integration tests",
                IsActive = true
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
        }

        var program = new GameGuild.Modules.Programs.Models.Program
        {
            Id = Guid.NewGuid(),
            Title = $"Test Program {slug}",
            Slug = slug,
            Description = $"Test description for {slug}",
            Status = status,
            Visibility = visibility,
            Category = ProgramCategory.Programming,
            Difficulty = ProgramDifficulty.Beginner,
            EstimatedHours = 10.0f,
            EnrollmentStatus = EnrollmentStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tenant = tenant
        };

        _context.Set<GameGuild.Modules.Programs.Models.Program>().Add(program);
        await _context.SaveChangesAsync();

        return program;
    }

    #endregion

    public void Dispose()
    {
        _scope?.Dispose();
        _factory?.Dispose();
    }
}
