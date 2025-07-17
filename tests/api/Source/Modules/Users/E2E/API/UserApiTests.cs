using System.Net;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Tests.Modules.Users.E2E.API {
  public class UserApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable {
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient? _client;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;
    private Guid _tenantId;
    private Guid _userId;

    public UserApiTests(WebApplicationFactory<Program> factory) {
      // Use a unique database name for this test class to avoid interference
      var uniqueDbName = $"UserApiTests_{Guid.NewGuid()}";
      _factory = IntegrationTestHelper.GetTestFactory(uniqueDbName);
      
      _scope = _factory.Services.CreateScope();
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
    public async Task Should_Register_And_Login_User() {
      // Arrange
      var unauthenticatedClient = _factory.CreateClient(); // Use unauthenticated client for registration
      var email = $"test.{Guid.NewGuid()}@example.com";
      var username = "testuser123"; // Simple, guaranteed valid username
      var registerPayload = new { username = username, email = email, password = "TestPassword123!" };
      
      Console.WriteLine($"Test payload: username={username}, email={email}");

      // Act - Register (should not require authentication)
      var registerResponse = await unauthenticatedClient.PostAsync(
                               "/api/auth/signup",
                               new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json")
                             );

      // Debug: Log response content if registration fails
      if (registerResponse.StatusCode != HttpStatusCode.Created) {
        var errorContent = await registerResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Registration failed with status: {registerResponse.StatusCode}");
        Console.WriteLine($"ErrorMessage content: {errorContent}");
      }

      // Assert - Register
      Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

      // Arrange - Login (also use unauthenticated client)
      var loginPayload = new { email = email, password = "TestPassword123!" };

      // Act - Login
      var loginResponse = await unauthenticatedClient.PostAsync(
                            "/api/auth/signin",
                            new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json")
                          );
      var loginContent = await loginResponse.Content.ReadAsStringAsync();
      var loginResult = JsonDocument.Parse(loginContent);

      // Assert - Login
      Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
      Assert.True(loginResult.RootElement.TryGetProperty("accessToken", out _));
      Assert.True(loginResult.RootElement.TryGetProperty("user", out var userElement));
      Assert.Equal(email, userElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Should_Get_User_By_Id() {
      // Arrange - Create authenticated client  
      _client = await GetAuthenticatedClientAsync();

      // Act
      var response = await _client.GetAsync($"/api/users/{_userId}");
      var content = await response.Content.ReadAsStringAsync();
      
      // Debug: Log the actual response
      if (string.IsNullOrEmpty(content)) {
        Assert.True(false, $"Response content is empty. Status: {response.StatusCode}, Headers: {response.Headers}");
      }
      
      var result = JsonDocument.Parse(content);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(_userId.ToString(), result.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Should_Update_User() {
      // Arrange - Create authenticated client
      _client = await GetAuthenticatedClientAsync();

      var updatePayload = new { name = "Updated Name" };

      // Act
      var response = await _client.PutAsync(
                       $"/api/users/{_userId}",
                       new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json")
                     );
      var content = await response.Content.ReadAsStringAsync();
      
      // Debug: Log the actual response
      if (string.IsNullOrEmpty(content)) {
        Assert.True(false, $"Response content is empty. Status: {response.StatusCode}, Headers: {response.Headers}");
      }
      
      var result = JsonDocument.Parse(content);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal("Updated Name", result.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Should_Delete_User() {
      // Arrange - Create authenticated client
      _client = await GetAuthenticatedClientAsync();

      // Act - Delete
      var deleteResponse = await _client.DeleteAsync($"/api/users/{_userId}");

      // Assert - Delete
      Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

      // Act - Try to get deleted user
      var getResponse = await _client.GetAsync($"/api/users/{_userId}");

      // Assert - Should not find deleted user
      Assert.Equal(HttpStatusCode.PageNotFound, getResponse.StatusCode);
    }

    public void Dispose() {
      _scope.Dispose();
      _context.Dispose();
      _client?.Dispose();
    }
  }
}
