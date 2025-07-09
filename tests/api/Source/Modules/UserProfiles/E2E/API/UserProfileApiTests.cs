using System.Net;
using System.Text;
using System.Text.Json;
using GameGuild.API.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.Tests.Modules.UserProfiles.E2E.API {
  public class UserProfileApiTests : IClassFixture<TestServerFixture> {
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;

    public UserProfileApiTests(TestServerFixture fixture) {
      _fixture = fixture;
      _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Should_Create_And_Update_UserProfile_Via_Api() {
      // Arrange - Create test user
      var userId = Guid.NewGuid().ToString();
      var email = $"profile-create-{Guid.NewGuid()}@example.com";
      await _fixture.SeedTestUser(userId, email, "Profile Test User");

      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      // Create profile payload
      var createPayload = new { bio = "My test bio", avatarUrl = "https://example.com/avatar.jpg", location = "Test City", websiteUrl = "https://example.com" };

      // Act - Create profile
      var createResponse = await _client.PostAsync(
                             $"/api/users/{userId}/profile",
                             new StringContent(JsonSerializer.Serialize(createPayload), Encoding.UTF8, "application/json")
                           );

      var createContent = await createResponse.Content.ReadAsStringAsync();
      var createResult = JsonDocument.Parse(createContent);
      var profileId = createResult.RootElement.GetProperty("id").GetString();

      // Assert - Create
      Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
      Assert.Equal("My test bio", createResult.RootElement.GetProperty("bio").GetString());
      Assert.Equal("https://example.com/avatar.jpg", createResult.RootElement.GetProperty("avatarUrl").GetString());

      // Arrange - Update
      var updatePayload = new { bio = "Updated bio", avatarUrl = "https://example.com/new-avatar.jpg" };

      // Act - Update
      var updateResponse = await _client.PutAsync(
                             $"/api/profiles/{profileId}",
                             new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json")
                           );

      var updateContent = await updateResponse.Content.ReadAsStringAsync();
      var updateResult = JsonDocument.Parse(updateContent);

      // Assert - Update
      Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
      Assert.Equal("Updated bio", updateResult.RootElement.GetProperty("bio").GetString());
      Assert.Equal("https://example.com/new-avatar.jpg", updateResult.RootElement.GetProperty("avatarUrl").GetString());
    }

    [Fact]
    public async Task Should_Get_UserProfile_By_UserId() {
      // Arrange - Create test user with profile
      var userId = Guid.NewGuid().ToString();
      var email = $"profile-get-{Guid.NewGuid()}@example.com";
      var name = "Profile Get User";
      var bio = "Get profile bio";
      var avatarUrl = "https://example.com/get-avatar.jpg";

      await _fixture.SeedTestUserWithProfile(userId, email, name, bio, avatarUrl);

      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      // Act
      var response = await _client.GetAsync($"/api/users/{userId}/profile");
      var content = await response.Content.ReadAsStringAsync();
      var result = JsonDocument.Parse(content);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(userId, result.RootElement.GetProperty("userId").GetString());
      Assert.Equal(bio, result.RootElement.GetProperty("bio").GetString());
      Assert.Equal(avatarUrl, result.RootElement.GetProperty("avatarUrl").GetString());
    }

    [Fact]
    public async Task Should_Return_NotFound_For_Nonexistent_Profile() {
      // Arrange
      var nonexistentId = Guid.NewGuid().ToString();

      // Set authentication token
      var token = _fixture.GenerateTestToken();
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      // Act
      var response = await _client.GetAsync($"/api/profiles/{nonexistentId}");

      // Assert
      Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_Delete_UserProfile() {
      // Arrange - Create test user with profile
      var userId = Guid.NewGuid().ToString();
      var email = $"profile-delete-{Guid.NewGuid()}@example.com";
      var name = "Profile Delete User";
      var bio = "Delete profile bio";
      var avatarUrl = "https://example.com/delete-avatar.jpg";

      await _fixture.SeedTestUserWithProfile(userId, email, name, bio, avatarUrl);

      // Get profile ID (in a real scenario, you would query this from the database)
      using var scope = _fixture.Server.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
      var profileId = dbContext.UserProfiles.Find(userId)?.Id ?? throw new Exception("Profile not found");

      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      // Act - Delete
      var deleteResponse = await _client.DeleteAsync($"/api/profiles/{profileId}");

      // Assert - Delete
      Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

      // Act - Try to get deleted profile
      var getResponse = await _client.GetAsync($"/api/profiles/{profileId}");

      // Assert - Should not find deleted profile
      Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
  }
}
