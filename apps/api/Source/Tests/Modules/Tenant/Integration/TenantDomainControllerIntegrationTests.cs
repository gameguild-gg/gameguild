using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GameGuild.Data;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.Auth.Services;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;
using GameGuild.Modules.Tenant.Dtos;
using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.User.Models;
using GameGuild.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TenantModel = GameGuild.Modules.Tenant.Models.Tenant;


namespace GameGuild.Tests.Modules.Tenant.Integration;

public class TenantDomainControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable {
  private readonly WebApplicationFactory<Program> _factory;
  private readonly HttpClient _client;
  private readonly IServiceScope _scope;
  private readonly ApplicationDbContext _context;
  private readonly Guid _tenantId = Guid.NewGuid();
  private readonly Guid _userId = Guid.NewGuid();

  public TenantDomainControllerIntegrationTests(WebApplicationFactory<Program> factory) {
    // Use a unique database name for this test class to avoid interference
    var uniqueDbName = $"TenantDomainIntegrationTests_{Guid.NewGuid()}";
    _factory = IntegrationTestHelper.GetTestFactory(uniqueDbName);
    _client = _factory.CreateClient();
    _scope = _factory.Services.CreateScope();
    _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
  }

  private async Task SetupTestData() {
    // Ensure the database is clean and recreated before each test
    if (Microsoft.EntityFrameworkCore.InMemoryDatabaseFacadeExtensions.IsInMemory(_context.Database)) {
      // Clean and recreate the database to avoid test interference
      await _context.Database.EnsureDeletedAsync();
      await _context.Database.EnsureCreatedAsync();
    }

    // Check if data already exists
    if (await _context.Tenants.AnyAsync(t => t.Id == _tenantId)) return;

    // Create test tenant
    var tenant = new TenantModel {
      Id = _tenantId,
      Name = "Test Tenant",
      Description = "Test tenant for domain integration tests",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _context.Tenants.Add(tenant);

    // Create test user
    var user = new User {
      Id = _userId,
      Name = "Test User",
      Email = "test@example.com",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _context.Users.Add(user);

    await _context.SaveChangesAsync();
  }

  #region Domain API Tests

  [Fact]
  public async Task CreateDomain_WithValidData_ReturnsCreatedDomain() {
    // Arrange
    await SetupAuthentication();

    var createDto = new CreateTenantDomainDto {
      TenantId = _tenantId,
      TopLevelDomain = "example.com",
      Subdomain = null,
      IsMainDomain = true,
      IsSecondaryDomain = false,
    };

    var json = JsonSerializer.Serialize(createDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/tenant-domains", content);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<TenantDomainDto>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(createDto.TopLevelDomain, result.TopLevelDomain);
    Assert.Equal(createDto.Subdomain, result.Subdomain);
    Assert.Equal(createDto.IsMainDomain, result.IsMainDomain);
    Assert.Equal(createDto.TenantId, result.TenantId);

    // Verify it was saved to database
    var savedDomain = await _context.TenantDomains.FirstOrDefaultAsync(d => d.Id == result.Id);
    Assert.NotNull(savedDomain);
  }

  [Fact]
  public async Task CreateDomain_WithSubdomain_ReturnsCreatedDomain() {
    // Arrange
    await SetupAuthentication();

    var createDto = new CreateTenantDomainDto {
      TenantId = _tenantId,
      TopLevelDomain = "university.edu",
      Subdomain = "cs",
      IsMainDomain = false,
      IsSecondaryDomain = true,
    };

    var json = JsonSerializer.Serialize(createDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/tenant-domains", content);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<TenantDomainDto>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal("university.edu", result.TopLevelDomain);
    Assert.Equal("cs", result.Subdomain);
    Assert.False(result.IsMainDomain);
    Assert.True(result.IsSecondaryDomain);
  }

  [Fact]
  public async Task GetDomainsByTenant_WithExistingDomains_ReturnsCorrectDomains() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestData();

    var domain1 = new TenantDomain {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      TopLevelDomain = "example.com",
      Subdomain = null,
      IsMainDomain = true,
      IsSecondaryDomain = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var domain2 = new TenantDomain {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      TopLevelDomain = "university.edu",
      Subdomain = "cs",
      IsMainDomain = false,
      IsSecondaryDomain = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantDomains.AddRange(domain1, domain2);
    await db.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync($"/api/tenant-domains/tenant/{_tenantId}");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<List<TenantDomainDto>>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.Contains(result, d => d.TopLevelDomain == "example.com");
    Assert.Contains(result, d => d.TopLevelDomain == "university.edu" && d.Subdomain == "cs");
  }

  [Fact]
  public async Task UpdateDomain_WithValidData_ReturnsUpdatedDomain() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestData();

    var domain = new TenantDomain {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      TopLevelDomain = "example.com",
      Subdomain = null,
      IsMainDomain = true,
      IsSecondaryDomain = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantDomains.Add(domain);
    await db.SaveChangesAsync();

    var updateDto = new UpdateTenantDomainDto { IsMainDomain = false, IsSecondaryDomain = true };

    var json = JsonSerializer.Serialize(updateDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PutAsync($"/api/tenant-domains/{domain.Id}", content);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<TenantDomainDto>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.False(result.IsMainDomain);
    Assert.True(result.IsSecondaryDomain);

    // Verify it was updated in database
    var updatedDomain = await db.TenantDomains.FirstOrDefaultAsync(d => d.Id == domain.Id);
    Assert.NotNull(updatedDomain);
    Assert.False(updatedDomain.IsMainDomain);
    Assert.True(updatedDomain.IsSecondaryDomain);
  }

  [Fact]
  public async Task DeleteDomain_WithValidId_ReturnsNoContent() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestData();

    var domain = new TenantDomain {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      TopLevelDomain = "example.com",
      Subdomain = null,
      IsMainDomain = true,
      IsSecondaryDomain = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantDomains.Add(domain);
    await db.SaveChangesAsync();

    // Act
    var response = await _client.DeleteAsync($"/api/tenant-domains/{domain.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    // Verify it was deleted from database
    var deletedDomain = await db.TenantDomains.FirstOrDefaultAsync(d => d.Id == domain.Id);
    Assert.Null(deletedDomain);
  }

  #endregion

  #region User Group API Tests

  [Fact]
  public async Task CreateUserGroup_WithValidData_ReturnsCreatedGroup() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestData();

    var createDto = new CreateTenantUserGroupDto {
      TenantId = _tenantId,
      Name = "Students",
      Description = "Student group",
      IsDefault = true,
      ParentGroupId = null,
    };

    var json = JsonSerializer.Serialize(createDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/tenant-domains/user-groups", content);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<TenantUserGroupDto>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(createDto.Name, result.Name);
    Assert.Equal(createDto.Description, result.Description);
    Assert.Equal(createDto.IsDefault, result.IsDefault);
    Assert.Equal(createDto.TenantId, result.TenantId);

    // Verify it was saved to database
    var savedGroup = await db.TenantUserGroups.FirstOrDefaultAsync(g => g.Id == result.Id);
    Assert.NotNull(savedGroup);
  }

  [Fact]
  public async Task GetUserGroupsByTenant_WithExistingGroups_ReturnsCorrectGroups() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestData();

    var group1 = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Students",
      Description = "Student group",
      IsDefault = true,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var group2 = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Faculty",
      Description = "Faculty group",
      IsDefault = false,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantUserGroups.AddRange(group1, group2);
    await db.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync($"/api/tenant-domains/user-groups/tenant/{_tenantId}");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<List<TenantUserGroupDto>>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.Contains(result, g => g.Name == "Students");
    Assert.Contains(result, g => g.Name == "Faculty");
  }

  #endregion

  #region Membership API Tests

  [Fact]
  public async Task AssignUserToGroup_WithValidData_ReturnsCreatedMembership() {
    // Arrange
    await SetupAuthentication(); // This already calls SetupTestData internally

    // Create a group in the database
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var group = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Students",
      Description = "Student group",
      IsDefault = true,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantUserGroups.Add(group);
    await db.SaveChangesAsync();

    var assignDto = new AddUserToGroupDto { UserId = _userId, UserGroupId = group.Id, IsAutoAssigned = false };

    var json = JsonSerializer.Serialize(assignDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/tenant-domains/memberships", content);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<TenantUserGroupMembershipDto>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(assignDto.UserId, result.UserId);
    Assert.Equal(assignDto.UserGroupId, result.GroupId);
    Assert.Equal(assignDto.IsAutoAssigned, result.IsAutoAssigned);

    // Verify it was saved to database using a fresh scope
    using var verifyScope = _factory.Services.CreateScope();
    var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var savedMembership = await verifyDb.TenantUserGroupMemberships.FirstOrDefaultAsync(m => m.Id == result.Id);
    Assert.NotNull(savedMembership);
  }

  [Fact]
  public async Task GetUserGroupMemberships_WithExistingMemberships_ReturnsCorrectMemberships() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestData();

    var group1 = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Students",
      Description = "Student group",
      IsDefault = true,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var group2 = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Faculty",
      Description = "Faculty group",
      IsDefault = false,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantUserGroups.AddRange(group1, group2);

    var membership1 = new TenantUserGroupMembership {
      Id = Guid.NewGuid(),
      UserId = _userId,
      UserGroupId = group1.Id,
      IsAutoAssigned = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var membership2 = new TenantUserGroupMembership {
      Id = Guid.NewGuid(),
      UserId = _userId,
      UserGroupId = group2.Id,
      IsAutoAssigned = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantUserGroupMemberships.AddRange(membership1, membership2);
    await db.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync($"/api/tenant-domains/memberships/user/{_userId}");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<List<TenantUserGroupMembershipDto>>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.Contains(result, m => m.GroupId == group1.Id);
    Assert.Contains(result, m => m.GroupId == group2.Id);
  }

  #endregion

  #region Auto-Assignment API Tests

  [Fact]
  public async Task AutoAssignUser_WithMatchingDomain_ReturnsCreatedMembership() {
    // Arrange
    await SetupAuthentication(); // Only call this, as it calls SetupTestData internally
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var domain = new TenantDomain {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      TopLevelDomain = "university.edu",
      Subdomain = null,
      IsMainDomain = true,
      IsSecondaryDomain = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var group = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Students",
      Description = "Student group",
      IsDefault = true,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantDomains.Add(domain);
    db.TenantUserGroups.Add(group);
    await db.SaveChangesAsync();

    // Link domain to group manually in database for this test
    domain.UserGroupId = group.Id;
    db.Update(domain);
    await db.SaveChangesAsync();

    var autoAssignDto = new AutoAssignUserDto { UserId = _userId, Email = "test@university.edu" };

    var json = JsonSerializer.Serialize(autoAssignDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<TenantUserGroupMembershipDto>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(_userId, result.UserId);
    Assert.Equal(group.Id, result.GroupId);
    Assert.True(result.IsAutoAssigned);

    // Verify it was saved to database
    var savedMembership =
      await db.TenantUserGroupMemberships.FirstOrDefaultAsync(m => m.UserId == _userId && m.UserGroupId == group.Id);
    Assert.NotNull(savedMembership);
    Assert.True(savedMembership.IsAutoAssigned);
  }

  [Fact]
  public async Task AutoAssignUser_WithNoMatchingDomain_ReturnsNotFound() {
    // Arrange
    await SetupAuthentication(); // Only call this, as it calls SetupTestData internally

    var autoAssignDto = new AutoAssignUserDto { UserId = _userId, Email = "test@nonexistent.com" };

    var json = JsonSerializer.Serialize(autoAssignDto);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/tenant-domains/auto-assign", content);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    // Verify no membership was created
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var membership = await db.TenantUserGroupMemberships.FirstOrDefaultAsync(m => m.UserId == _userId);
    Assert.Null(membership);
  }

  #endregion

  #region Query API Tests

  [Fact]
  public async Task GetUsersByGroup_WithExistingMemberships_ReturnsCorrectUsers() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestData();

    var user2 = new User {
      Id = Guid.NewGuid(),
      Name = "Test User 2",
      Email = "test2@example.com",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.Users.Add(user2);

    var group = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Students",
      Description = "Student group",
      IsDefault = true,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantUserGroups.Add(group);

    var membership1 = new TenantUserGroupMembership {
      Id = Guid.NewGuid(),
      UserId = _userId,
      UserGroupId = group.Id,
      IsAutoAssigned = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var membership2 = new TenantUserGroupMembership {
      Id = Guid.NewGuid(),
      UserId = user2.Id,
      UserGroupId = group.Id,
      IsAutoAssigned = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantUserGroupMemberships.AddRange(membership1, membership2);
    await db.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync($"/api/tenant-domains/groups/{group.Id}/users");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<List<UserDto>>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.Contains(result, u => u.Id == _userId);
    Assert.Contains(result, u => u.Id == user2.Id);
  }

  [Fact]
  public async Task GetGroupsByUser_WithExistingMemberships_ReturnsCorrectGroups() {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestData();

    var group1 = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Students",
      Description = "Student group",
      IsDefault = true,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var group2 = new TenantUserGroup {
      Id = Guid.NewGuid(),
      TenantId = _tenantId,
      Name = "Faculty",
      Description = "Faculty group",
      IsDefault = false,
      ParentGroupId = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantUserGroups.AddRange(group1, group2);

    var membership1 = new TenantUserGroupMembership {
      Id = Guid.NewGuid(),
      UserId = _userId,
      UserGroupId = group1.Id,
      IsAutoAssigned = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var membership2 = new TenantUserGroupMembership {
      Id = Guid.NewGuid(),
      UserId = _userId,
      UserGroupId = group2.Id,
      IsAutoAssigned = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantUserGroupMemberships.AddRange(membership1, membership2);
    await db.SaveChangesAsync();

    // Act
    var response = await _client.GetAsync($"/api/tenant-domains/users/{_userId}/groups");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseContent = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<List<TenantUserGroupDto>>(
      responseContent,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.Contains(result, g => g.Id == group1.Id);
    Assert.Contains(result, g => g.Id == group2.Id);
  }

  #endregion

  #region Helper Methods

  private async Task SetupAuthentication() {
    // Create test user and tenant (if not already created)
    await SetupTestData();

    // Get the user and tenant entities
    var user = await _context.Users.FirstAsync(u => u.Id == _userId);
    var tenant = await _context.Tenants.FirstAsync(t => t.Id == _tenantId);

    // Grant permissions for TenantDomain operations
    var permissionService = _scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permissionService.GrantContentTypePermissionAsync(
      user.Id,
      tenant.Id,
      "TenantDomain",
      new[] { PermissionType.Read, PermissionType.Create, PermissionType.Delete }
    );
    // Grant permissions for TenantUserGroupMembership operations
    await permissionService.GrantContentTypePermissionAsync(
      user.Id,
      tenant.Id,
      "TenantUserGroupMembership",
      new[] { PermissionType.Read, PermissionType.Create, PermissionType.Delete }
    );

    // Generate JWT token
    var token = CreateJwtTokenForUserAsync(user, tenant);

    // Set authorization header
    SetAuthorizationHeader(token);
  }

  private string CreateJwtTokenForUserAsync(User user, TenantModel tenant) {
    var jwtService = _scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

    var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };

    var roles = new[] { "User" };
    var additionalClaims = new[] { new Claim("tenant_id", tenant.Id.ToString()) };

    return jwtService.GenerateAccessToken(userDto, roles, additionalClaims);
  }

  private void SetAuthorizationHeader(string token) { _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); }

  private void ClearAuthorizationHeader() { _client.DefaultRequestHeaders.Authorization = null; }

  public void Dispose() {
    _scope.Dispose();
    _context.Dispose();
    _client.Dispose();
  }

  #endregion
}
