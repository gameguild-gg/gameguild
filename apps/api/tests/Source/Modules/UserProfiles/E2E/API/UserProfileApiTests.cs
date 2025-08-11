using System.Net;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Tests.Modules.UserProfiles.E2E.API {
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
      var createPayload = new { 
        userId = userId, 
        givenName = "Test", 
        familyName = "User", 
        displayName = "Test User", 
        title = "Developer",
        description = "My test bio",
      };

      // Act - Create profile
      var createResponse = await _client.PostAsync(
                             "/api/userprofiles",
                             new StringContent(JsonSerializer.Serialize(createPayload), Encoding.UTF8, "application/json")
                           );

      // Assert - Create response status first
      var createContent = await createResponse.Content.ReadAsStringAsync();
      Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
      
      // Ensure we have valid JSON content
      Assert.False(string.IsNullOrWhiteSpace(createContent), $"Expected valid JSON response but got: '{createContent}'");
      
      var createResult = JsonDocument.Parse(createContent);
      var profileId = createResult.RootElement.GetProperty("id").GetString();

      // Assert - Create properties
      Assert.Equal("Test", createResult.RootElement.GetProperty("givenName").GetString());
      Assert.Equal("User", createResult.RootElement.GetProperty("familyName").GetString());
      Assert.Equal("Test User", createResult.RootElement.GetProperty("displayName").GetString());
      Assert.Equal("My test bio", createResult.RootElement.GetProperty("description").GetString());

      // Arrange - Update
      var updatePayload = new { 
        givenName = "Updated", 
        familyName = "User", 
        displayName = "Updated User",
        description = "Updated bio",
      };

      // Act - Update
      var updateResponse = await _client.PutAsync(
                             $"/api/userprofiles/{profileId}",
                             new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json")
                           );

      // Assert - Update response status first
      var updateContent = await updateResponse.Content.ReadAsStringAsync();
      Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
      
      // Ensure we have valid JSON content
      Assert.False(string.IsNullOrWhiteSpace(updateContent), $"Expected valid JSON response but got: '{updateContent}'");
      
      var updateResult = JsonDocument.Parse(updateContent);

      // Assert - Update properties
      Assert.Equal("Updated", updateResult.RootElement.GetProperty("givenName").GetString());
      Assert.Equal("Updated User", updateResult.RootElement.GetProperty("displayName").GetString());
      Assert.Equal("Updated bio", updateResult.RootElement.GetProperty("description").GetString());
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
      var response = await _client.GetAsync($"/api/UserProfiles/user/{userId}");
      var content = await response.Content.ReadAsStringAsync();
      var result = JsonDocument.Parse(content);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal(userId, result.RootElement.GetProperty("id").GetString());
      Assert.Equal("Get profile bio", result.RootElement.GetProperty("description").GetString());
      Assert.Equal("Profile Get User", result.RootElement.GetProperty("displayName").GetString());
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
      var response = await _client.GetAsync($"/api/UserProfiles/{nonexistentId}");

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
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var userGuid = Guid.Parse(userId);
      var userProfile = dbContext.UserProfiles.FirstOrDefault(p => p.Id == userGuid);
      var profileId = userProfile?.Id ?? throw new Exception("Profile not found");

      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      // Act - Delete
      var deleteResponse = await _client.DeleteAsync($"/api/UserProfiles/{profileId}");

      // Assert - Delete
      Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

      // Act - Try to get deleted profile
      var getResponse = await _client.GetAsync($"/api/UserProfiles/{profileId}");

      // Assert - Should not find deleted profile
      Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
  }
}
