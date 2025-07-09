using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.Common.Enums;
using GameGuild.Data;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.Auth.Services;
using GameGuild.Modules.Contents.Models;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Users.Models;
using GameGuild.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using TenantModel = GameGuild.Modules.Tenants.Models.Tenant;
using ProgramEntity = GameGuild.Modules.Programs.Models.Program;

namespace GameGuild.Tests.Modules.Programs.Integration.Controllers;

/// <summary>
/// Integration tests for ProgramController focusing on slug-based operations
/// Tests HTTP endpoints with real database and full application stack
/// </summary>
public class ProgramControllerSlugTests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private readonly ApplicationDbContext _context;
  private readonly IServiceScope _scope;
  private readonly ITestOutputHelper _output;

  public ProgramControllerSlugTests(TestWebApplicationFactory factory, ITestOutputHelper output) {
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
  public async Task GetProgramBySlug_WithValidSlug_ReturnsProgram() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    var authToken = await AuthenticateUserAsync(tenantUser.User.Email, "password123");
    var program = await CreateTestProgramAsync(tenantUser.Tenant.Id, "test-program-slug");

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

    // Act
    var response = await _client.GetAsync($"/api/program/slug/{program.Slug}");

    // Assert
    response.EnsureSuccessStatusCode();
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var returnedProgram = JsonSerializer.Deserialize<ProgramEntity>(responseContent, new JsonSerializerOptions {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    Assert.NotNull(returnedProgram);
    Assert.Equal(program.Slug, returnedProgram.Slug);
    Assert.Equal(program.Title, returnedProgram.Title);
    Assert.Equal(program.Description, returnedProgram.Description);
  }

  [Fact]
  public async Task GetProgramBySlug_WithInvalidSlug_ReturnsNotFound() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    var authToken = await AuthenticateUserAsync(tenantUser.User.Email, "password123");

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

    // Act
    var response = await _client.GetAsync("/api/program/slug/non-existent-slug");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetProgramBySlug_WithPublishedProgram_ReturnsSuccessWithoutAuth() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    var program = await CreateTestProgramAsync(tenantUser.Tenant.Id, "public-program-slug", 
      status: ContentStatus.Published, visibility: ContentVisibility.Public);

    // Act - No authentication header
    var response = await _client.GetAsync($"/api/program/slug/{program.Slug}");

    // Assert
    response.EnsureSuccessStatusCode();
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var returnedProgram = JsonSerializer.Deserialize<ProgramEntity>(responseContent, new JsonSerializerOptions {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    Assert.NotNull(returnedProgram);
    Assert.Equal(program.Slug, returnedProgram.Slug);
  }

  [Fact]
  public async Task GetProgramBySlug_WithDraftProgram_RequiresAuthentication() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    var program = await CreateTestProgramAsync(tenantUser.Tenant.Id, "draft-program-slug", 
      status: ContentStatus.Draft, visibility: ContentVisibility.Private);

    // Act - No authentication header
    var response = await _client.GetAsync($"/api/program/slug/{program.Slug}");

    // Assert
    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetPublishedPrograms_ReturnsOnlyPublishedPrograms() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    
    // Create multiple programs with different statuses
    var publishedProgram1 = await CreateTestProgramAsync(tenantUser.Tenant.Id, "published-1", 
      status: ContentStatus.Published, visibility: ContentVisibility.Public);
    var publishedProgram2 = await CreateTestProgramAsync(tenantUser.Tenant.Id, "published-2", 
      status: ContentStatus.Published, visibility: ContentVisibility.Public);
    var draftProgram = await CreateTestProgramAsync(tenantUser.Tenant.Id, "draft-1", 
      status: ContentStatus.Draft, visibility: ContentVisibility.Private);

    // Act
    var response = await _client.GetAsync("/api/program/published");

    // Assert
    response.EnsureSuccessStatusCode();
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var programs = JsonSerializer.Deserialize<List<ProgramEntity>>(responseContent, new JsonSerializerOptions {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    Assert.NotNull(programs);
    Assert.Equal(2, programs.Count);
    Assert.Contains(programs, p => p.Slug == publishedProgram1.Slug);
    Assert.Contains(programs, p => p.Slug == publishedProgram2.Slug);
    Assert.DoesNotContain(programs, p => p.Slug == draftProgram.Slug);
  }

  [Fact]
  public async Task GetProgramBySlug_WithSpecialCharactersInSlug_HandlesCorrectly() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    var authToken = await AuthenticateUserAsync(tenantUser.User.Email, "password123");
    var program = await CreateTestProgramAsync(tenantUser.Tenant.Id, "unity-3d-game-development-2024");

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

    // Act
    var response = await _client.GetAsync($"/api/program/slug/{program.Slug}");

    // Assert
    response.EnsureSuccessStatusCode();
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var returnedProgram = JsonSerializer.Deserialize<ProgramEntity>(responseContent, new JsonSerializerOptions {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    Assert.NotNull(returnedProgram);
    Assert.Equal(program.Slug, returnedProgram.Slug);
  }

  #region Helper Methods

  private void ClearDatabase() {
    try {
      _context.Database.EnsureDeleted();
      _context.Database.EnsureCreated();
    }
    catch (Exception ex) {
      _output.WriteLine($"Warning: Could not clear database: {ex.Message}");
    }
  }

  private async Task<(TenantModel Tenant, User User)> CreateTenantWithUserAsync() {
    var tenant = new TenantModel {
      Id = Guid.NewGuid(),
      Name = "Test Tenant",
      Slug = $"test-tenant-{Guid.NewGuid():N}",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    var user = new User {
      Id = Guid.NewGuid(),
      TenantId = tenant.Id,
      Username = $"testuser_{Guid.NewGuid():N}",
      Email = $"test_{Guid.NewGuid():N}@example.com",
      PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Set<TenantModel>().Add(tenant);
    _context.Set<User>().Add(user);
    await _context.SaveChangesAsync();

    return (tenant, user);
  }

  private async Task<string> AuthenticateUserAsync(string email, string password) {
    var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
    
    var loginRequest = new LoginRequest {
      Email = email,
      Password = password
    };

    var result = await authService.LoginAsync(loginRequest);
    
    if (!result.IsSuccess || result.Data == null) {
      throw new InvalidOperationException($"Authentication failed: {result.ErrorMessage}");
    }

    return result.Data.AccessToken;
  }

  private async Task<ProgramEntity> CreateTestProgramAsync(Guid tenantId, string slug, 
    ContentStatus status = ContentStatus.Draft, 
    ContentVisibility visibility = ContentVisibility.Private) {
    
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      TenantId = tenantId,
      Title = $"Test Program {slug}",
      Slug = slug,
      Description = $"Test description for {slug}",
      Status = status,
      Visibility = visibility,
      Category = "programming",
      Difficulty = ProgramDifficulty.Beginner,
      EstimatedHours = 10.0f,
      EnrollmentStatus = EnrollmentStatus.Open,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _context.Set<ProgramEntity>().Add(program);
    await _context.SaveChangesAsync();

    return program;
  }

  #endregion

  public void Dispose() {
    _scope?.Dispose();
    _client?.Dispose();
  }
}
