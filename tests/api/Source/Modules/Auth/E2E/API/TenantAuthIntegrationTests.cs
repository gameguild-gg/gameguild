using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Tenants;
using GameGuild.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TenantModel = GameGuild.Modules.Tenants.Tenant;
using UserModel = GameGuild.Modules.Users.User;


namespace GameGuild.Tests.Modules.Auth.E2E.API;

public class TenantAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>> {
  private readonly WebApplicationFactory<Program> _factory;

  private readonly HttpClient _client;

  public TenantAuthIntegrationTests(WebApplicationFactory<Program> factory) {
    // Use a unique database name for this test class to avoid interference
    var uniqueDbName = $"TenantAuthIntegrationTests_{Guid.NewGuid()}";
    _factory = IntegrationTestHelper.GetTestFactory(uniqueDbName);
    _client = _factory.CreateClient();
  }

  private static async Task SetupTestTenantData(ApplicationDbContext db) {
    // Ensure the database is clean and recreated before each test
    if (InMemoryDatabaseFacadeExtensions.IsInMemory(db.Database)) {
      // Clean and recreate the database to avoid test interference
      await db.Database.EnsureDeletedAsync();
      await db.Database.EnsureCreatedAsync();
    }

    // Check if data already exists
    if (await db.Users.AnyAsync()) return;

    // Create test user
    var user = new UserModel {
      Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
      Name = "tenant-test",
      Email = "tenant@example.com",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.Users.Add(user);

    // Create password credential
    var credential = new Credential {
      Id = Guid.NewGuid(),
      UserId = user.Id,
      Type = "password",
      Value = Convert.ToBase64String(
        System.Security.Cryptography.SHA256.Create()
              .ComputeHash(Encoding.UTF8.GetBytes("P455W0RD"))
      ),
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.Credentials.Add(credential);

    // Create tenants
    var tenant1 = new TenantModel {
      Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
      Name = "Test Tenant 1",
      Description = "First test tenant",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var tenant2 = new TenantModel {
      Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
      Name = "Test Tenant 2",
      Description = "Second test tenant",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.Tenants.AddRange(tenant1, tenant2);

    // Create tenant permissions
    var tenantPermission1 = new TenantPermission {
      Id = Guid.NewGuid(),
      UserId = user.Id,
      TenantId = tenant1.Id,
      PermissionFlags1 = 1,
      PermissionFlags2 = 2,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var tenantPermission2 = new TenantPermission {
      Id = Guid.NewGuid(),
      UserId = user.Id,
      TenantId = tenant2.Id,
      PermissionFlags1 = 4,
      PermissionFlags2 = 8,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    db.TenantPermissions.AddRange(tenantPermission1, tenantPermission2);

    await db.SaveChangesAsync();
  }

  [Fact]
  public async Task Login_WithTenantId_ReturnsTenantInformationInResponse() {
    // Setup test data in the same scope as the test
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestTenantData(db);

    // Arrange
    var request = new LocalSignInRequestDto { Email = "tenant@example.com", Password = "P455W0RD", TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222") };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/auth/signin", content);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseData = await response.Content.ReadFromJsonAsync<SignInResponseDto>();
    Assert.NotNull(responseData);
    Assert.NotEmpty(responseData.AccessToken);
    Assert.NotEmpty(responseData.RefreshToken);
    Assert.Equal(request.TenantId, responseData.TenantId);
    Assert.NotNull(responseData.AvailableTenants);
    Assert.Equal(2, responseData.AvailableTenants.Count());
  }

  [Fact]
  public async Task Login_WithoutTenantId_ReturnsFirstTenant() {
    // Setup test data in the same scope as the test
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestTenantData(db);

    // Arrange
    var request = new LocalSignInRequestDto { Email = "tenant@example.com", Password = "P455W0RD" };

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/auth/signin", content);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var responseData = await response.Content.ReadFromJsonAsync<SignInResponseDto>();
    Assert.NotNull(responseData);
    Assert.NotEmpty(responseData.AccessToken);
    Assert.NotEmpty(responseData.RefreshToken);
    Assert.NotNull(responseData.TenantId);
    Assert.NotNull(responseData.AvailableTenants);
    Assert.Equal(2, responseData.AvailableTenants.Count());
  }

  [Fact]
  public async Task RefreshToken_WithTenantId_ReturnsTenantSpecificToken() {
    // Setup test data in the same scope as the test
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SetupTestTenantData(db);

    // First login to get a refresh token
    var loginRequest = new LocalSignInRequestDto { Email = "tenant@example.com", Password = "P455W0RD" };

    var loginJson = JsonSerializer.Serialize(loginRequest);
    var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

    var loginResponse = await _client.PostAsync("/api/auth/signin", loginContent);
    var loginData = await loginResponse.Content.ReadFromJsonAsync<SignInResponseDto>();

    Assert.NotNull(loginData);
    Assert.NotNull(loginData.RefreshToken);

    // Now refresh the token with a specific tenant ID
    var refreshRequest = new RefreshTokenRequestDto {
      RefreshToken = loginData.RefreshToken, TenantId = Guid.Parse("33333333-3333-3333-3333-333333333333"), // Use the second tenant
    };

    var refreshJson = JsonSerializer.Serialize(refreshRequest);
    var refreshContent = new StringContent(refreshJson, Encoding.UTF8, "application/json");

    // Act
    var refreshResponse = await _client.PostAsync("/api/auth/refresh", refreshContent);

    // Assert
    Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

    var responseData = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
    Assert.NotNull(responseData);
    Assert.NotEmpty(responseData.AccessToken);
    Assert.NotEmpty(responseData.RefreshToken);
    Assert.Equal(refreshRequest.TenantId, responseData.TenantId);
  }
}
