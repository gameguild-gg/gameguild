using System.Net;
using System.Text;
using System.Text.Json;
using GameGuild.API.Tests.Fixtures;


// using GameGuild.API.Models;

namespace GameGuild.API.Tests.Modules.Users.E2E.API {
  public class UserApiTests : IClassFixture<TestServerFixture> {
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;

    public UserApiTests(TestServerFixture fixture) {
      _fixture = fixture;
      _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Should_Register_And_Login_User() {
      // Arrange
      var email = $"test.{Guid.NewGuid()}@example.com";
      var registerPayload = new { name = "Test User", email = email, password = "TestPassword123!" };

      // Act - Register
      var registerResponse = await _client.PostAsync(
                               "/auth/sign-up",
                               new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json")
                             );

      // Assert - Register
      Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

      // Arrange - Login
      var loginPayload = new { email = email, password = "TestPassword123!" };

      // Act - Login
      var loginResponse = await _client.PostAsync(
                            "/auth/sign-in",
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
      // Arrange - Seed user directly to database
      var userId = Guid.NewGuid().ToString();
      var email = $"get-user-{Guid.NewGuid()}@example.com";
      var name = "Get Test User";
      
      await _fixture.SeedTestUser(userId, email, name);
      
      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      // Act
      var response = await _client.GetAsync($"/api/users/{userId}");
      var content = await response.Content.ReadAsStringAsync();
      
      // Debug: Log the actual response
      if (string.IsNullOrEmpty(content)) {
        Assert.True(false, $"Response content is empty. Status: {response.StatusCode}, Headers: {response.Headers}");
      }
      
      var result = JsonDocument.Parse(content);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(userId, result.RootElement.GetProperty("id").GetString());
      Assert.Equal(email, result.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Should_Update_User() {
      // Arrange - Create and seed test user
      var userId = Guid.NewGuid().ToString();
      var email = $"update-user-{Guid.NewGuid()}@example.com";
      await _fixture.SeedTestUser(userId, email, "Update Test User");

      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      var updatePayload = new { name = "Updated Name" };

      // Act
      var response = await _client.PutAsync(
                       $"/api/users/{userId}",
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
      // Arrange - Create and seed test user
      var userId = Guid.NewGuid().ToString();
      var email = $"delete-user-{Guid.NewGuid()}@example.com";
      await _fixture.SeedTestUser(userId, email, "Delete Test User");

      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      // Act - Delete
      var deleteResponse = await _client.DeleteAsync($"/api/users/{userId}");

      // Assert - Delete
      Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

      // Act - Try to get deleted user
      var getResponse = await _client.GetAsync($"/api/users/{userId}");

      // Assert - Should not find deleted user
      Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
  }
}
