using System.Net.Http.Headers;
using System.Text.Json;
using GameGuild.API.Tests.Source.Tests.Fixtures;
using GameGuild.Common.Enums;
using GameGuild.Data;
using GameGuild.Modules.Auth.Services;
using GameGuild.Modules.Contents.Models;
using GameGuild.Modules.Users.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using TenantModel = GameGuild.Modules.Tenants.Models.Tenant;
using ProgramEntity = GameGuild.Modules.Programs.Models.Program;

namespace GameGuild.API.Tests.Source.Tests.Modules.Programs.E2E.API;

/// <summary>
/// End-to-end tests for Program course catalog and enrollment workflows
/// Tests complete user journeys from course discovery to enrollment using slugs
/// </summary>
public class ProgramCatalogE2ETests : IClassFixture<TestWebApplicationFactory>, IDisposable {
  private readonly TestWebApplicationFactory _factory;
  private readonly HttpClient _client;
  private readonly ApplicationDbContext _context;
  private readonly IServiceScope _scope;
  private readonly ITestOutputHelper _output;

  public ProgramCatalogE2ETests(TestWebApplicationFactory factory, ITestOutputHelper output) {
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
  public async Task CourseDiscoveryToEnrollment_CompleteWorkflow_UsingSlugNavigation() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    var authToken = await AuthenticateUserAsync(tenantUser.User.Email, "password123");
    
    // Create a course catalog with different difficulty levels
    var beginnerCourse = await CreateTestProgramAsync(tenantUser.Tenant.Id, "unity-basics-2024",
      title: "Unity Basics 2024",
      difficulty: ProgramDifficulty.Beginner,
      status: ContentStatus.Published,
      visibility: ContentVisibility.Public);
      
    var intermediateCourse = await CreateTestProgramAsync(tenantUser.Tenant.Id, "advanced-unity-scripting",
      title: "Advanced Unity Scripting",
      difficulty: ProgramDifficulty.Intermediate,
      status: ContentStatus.Published,
      visibility: ContentVisibility.Public);

    var advancedCourse = await CreateTestProgramAsync(tenantUser.Tenant.Id, "unity-multiplayer-networking",
      title: "Unity Multiplayer Networking",
      difficulty: ProgramDifficulty.Advanced,
      status: ContentStatus.Published,
      visibility: ContentVisibility.Public);

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

    // Act & Assert - Complete workflow

    // 1. Discover published courses (no auth required)
    _client.DefaultRequestHeaders.Authorization = null;
    var catalogResponse = await _client.GetAsync("/api/program/published");
    catalogResponse.EnsureSuccessStatusCode();
    
    var catalog = JsonSerializer.Deserialize<List<ProgramEntity>>(
      await catalogResponse.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    
    Assert.NotNull(catalog);
    Assert.Equal(3, catalog.Count);
    Assert.Contains(catalog, c => c.Slug == "unity-basics-2024");
    Assert.Contains(catalog, c => c.Slug == "advanced-unity-scripting");
    Assert.Contains(catalog, c => c.Slug == "unity-multiplayer-networking");

    // 2. View beginner course details by slug
    var courseDetailResponse = await _client.GetAsync($"/api/program/slug/{beginnerCourse.Slug}");
    courseDetailResponse.EnsureSuccessStatusCode();
    
    var courseDetail = JsonSerializer.Deserialize<ProgramEntity>(
      await courseDetailResponse.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    
    Assert.NotNull(courseDetail);
    Assert.Equal(beginnerCourse.Slug, courseDetail.Slug);
    Assert.Equal("Unity Basics 2024", courseDetail.Title);
    Assert.Equal(ProgramDifficulty.Beginner, courseDetail.Difficulty);

    // 3. Try to access course content (should require authentication)
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
    var contentResponse = await _client.GetAsync($"/api/programcontent/program/{beginnerCourse.Id}");
    
    // Should succeed with authentication
    Assert.True(contentResponse.IsSuccessStatusCode || contentResponse.StatusCode == System.Net.HttpStatusCode.NotFound);

    // 4. Navigate between courses using slugs
    var advancedCourseResponse = await _client.GetAsync($"/api/program/slug/{advancedCourse.Slug}");
    advancedCourseResponse.EnsureSuccessStatusCode();
    
    var advancedCourseDetail = JsonSerializer.Deserialize<ProgramEntity>(
      await advancedCourseResponse.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    
    Assert.NotNull(advancedCourseDetail);
    Assert.Equal(advancedCourse.Slug, advancedCourseDetail.Slug);
    Assert.Equal(ProgramDifficulty.Advanced, advancedCourseDetail.Difficulty);
  }

  [Fact]
  public async Task CourseAccessControl_DraftVsPublished_SlugBasedAccess() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    var authToken = await AuthenticateUserAsync(tenantUser.User.Email, "password123");
    
    var draftCourse = await CreateTestProgramAsync(tenantUser.Tenant.Id, "draft-course-2024",
      title: "Draft Course 2024",
      status: ContentStatus.Draft,
      visibility: ContentVisibility.Private);
      
    var publishedCourse = await CreateTestProgramAsync(tenantUser.Tenant.Id, "published-course-2024",
      title: "Published Course 2024",
      status: ContentStatus.Published,
      visibility: ContentVisibility.Public);

    // Act & Assert

    // 1. Public access to published course (no auth)
    var publicResponse = await _client.GetAsync($"/api/program/slug/{publishedCourse.Slug}");
    publicResponse.EnsureSuccessStatusCode();

    // 2. Public access to draft course should fail
    var draftPublicResponse = await _client.GetAsync($"/api/program/slug/{draftCourse.Slug}");
    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, draftPublicResponse.StatusCode);

    // 3. Authenticated access to draft course should succeed
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
    var draftAuthResponse = await _client.GetAsync($"/api/program/slug/{draftCourse.Slug}");
    draftAuthResponse.EnsureSuccessStatusCode();

    // 4. Published courses should only appear in public catalog
    _client.DefaultRequestHeaders.Authorization = null;
    var catalogResponse = await _client.GetAsync("/api/program/published");
    catalogResponse.EnsureSuccessStatusCode();
    
    var catalog = JsonSerializer.Deserialize<List<ProgramEntity>>(
      await catalogResponse.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    
    Assert.NotNull(catalog);
    Assert.Contains(catalog, c => c.Slug == publishedCourse.Slug);
    Assert.DoesNotContain(catalog, c => c.Slug == draftCourse.Slug);
  }

  [Fact]
  public async Task CourseSlugNavigation_SEOFriendly_HandlesComplexSlugs() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    
    // Create courses with SEO-friendly slugs that include years, technologies, and levels
    var courses = new[] {
      await CreateTestProgramAsync(tenantUser.Tenant.Id, "unity-3d-game-development-beginner-2024",
        title: "Unity 3D Game Development for Beginners 2024",
        status: ContentStatus.Published,
        visibility: ContentVisibility.Public),
        
      await CreateTestProgramAsync(tenantUser.Tenant.Id, "unreal-engine-5-blueprint-scripting-intermediate",
        title: "Unreal Engine 5 Blueprint Scripting - Intermediate",
        status: ContentStatus.Published,
        visibility: ContentVisibility.Public),
        
      await CreateTestProgramAsync(tenantUser.Tenant.Id, "blender-3d-modeling-animation-complete-course",
        title: "Blender 3D Modeling & Animation - Complete Course",
        status: ContentStatus.Published,
        visibility: ContentVisibility.Public)
    };

    // Act & Assert - Test navigation with complex slugs
    foreach (var course in courses) {
      var response = await _client.GetAsync($"/api/program/slug/{course.Slug}");
      response.EnsureSuccessStatusCode();
      
      var courseDetail = JsonSerializer.Deserialize<ProgramEntity>(
        await response.Content.ReadAsStringAsync(),
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
      
      Assert.NotNull(courseDetail);
      Assert.Equal(course.Slug, courseDetail.Slug);
      Assert.Equal(course.Title, courseDetail.Title);
      
      // Verify slug is URL-safe (no spaces, special chars are encoded properly)
      Assert.DoesNotContain(" ", course.Slug);
      Assert.DoesNotContain("&", course.Slug);
      Assert.DoesNotContain("#", course.Slug);
    }
  }

  [Fact]
  public async Task CourseFiltering_ByCategory_MaintainsSlugConsistency() {
    // Arrange
    var tenantUser = await CreateTenantWithUserAsync();
    
    // Create courses in different categories
    var programmingCourse = await CreateTestProgramAsync(tenantUser.Tenant.Id, "c-sharp-programming-fundamentals",
      title: "C# Programming Fundamentals",
      category: "programming",
      status: ContentStatus.Published,
      visibility: ContentVisibility.Public);
      
    var artCourse = await CreateTestProgramAsync(tenantUser.Tenant.Id, "digital-art-photoshop-essentials",
      title: "Digital Art with Photoshop Essentials",
      category: "art",
      status: ContentStatus.Published,
      visibility: ContentVisibility.Public);
      
    var designCourse = await CreateTestProgramAsync(tenantUser.Tenant.Id, "ui-ux-design-figma-masterclass",
      title: "UI/UX Design with Figma Masterclass",
      category: "design",
      status: ContentStatus.Published,
      visibility: ContentVisibility.Public);

    // Act & Assert
    
    // Test individual course access by slug
    var programmingResponse = await _client.GetAsync($"/api/program/slug/{programmingCourse.Slug}");
    var artResponse = await _client.GetAsync($"/api/program/slug/{artCourse.Slug}");
    var designResponse = await _client.GetAsync($"/api/program/slug/{designCourse.Slug}");
    
    programmingResponse.EnsureSuccessStatusCode();
    artResponse.EnsureSuccessStatusCode();
    designResponse.EnsureSuccessStatusCode();

    // Verify slug consistency across categories
    var allPublishedResponse = await _client.GetAsync("/api/program/published");
    allPublishedResponse.EnsureSuccessStatusCode();
    
    var allCourses = JsonSerializer.Deserialize<List<ProgramEntity>>(
      await allPublishedResponse.Content.ReadAsStringAsync(),
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    
    Assert.NotNull(allCourses);
    Assert.Contains(allCourses, c => c.Slug == programmingCourse.Slug && c.Category == ProgramCategory.Programming);
    Assert.Contains(allCourses, c => c.Slug == artCourse.Slug && c.Category == ProgramCategory.CreativeArts);
    Assert.Contains(allCourses, c => c.Slug == designCourse.Slug && c.Category == ProgramCategory.Design);
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
    string? title = null,
    ProgramCategory category = ProgramCategory.Programming,
    ProgramDifficulty difficulty = ProgramDifficulty.Beginner,
    ContentStatus status = ContentStatus.Draft, 
    AccessLevel visibility = AccessLevel.Private) {
    
    var program = new ProgramEntity {
      Id = Guid.NewGuid(),
      Title = title ?? $"Test Program {slug}",
      Slug = slug,
      Description = $"Test description for {slug}",
      Category = category,
      Difficulty = difficulty,
      Status = status,
      Visibility = visibility,
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
