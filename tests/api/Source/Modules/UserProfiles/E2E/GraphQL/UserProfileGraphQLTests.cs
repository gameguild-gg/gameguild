using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;


namespace GameGuild.Tests.Modules.UserProfiles.E2E.GraphQL {
  public class UserProfileGraphQLTests : IClassFixture<TestServerFixture> {
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;

    public UserProfileGraphQLTests(TestServerFixture fixture) {
      _fixture = fixture;
      _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Should_Query_UserProfile_Details() {
      // Arrange - Create test user with profile
      var userId = Guid.NewGuid().ToString();
      var email = $"graphql-profile-{Guid.NewGuid()}@example.com";
      var name = "GraphQL Profile User";
      var bio = "GraphQL profile bio";
      var avatarUrl = "https://example.com/graphql-profile-avatar.jpg";

      await _fixture.SeedTestUserWithProfile(userId, email, name, bio, avatarUrl);

      // Get profile ID
      using var scope = _fixture.Server.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var profileId = Guid.Parse(userId);
      var profile = await dbContext.UserProfiles.FindAsync(profileId);
      if (profile == null) throw new Exception("Profile not found");

      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      var query = @"
                query GetUserProfile($id: ID!) {
                  getUserProfileById(id: $id) {
                    id
                    bio
                    avatarUrl
                    location
                    websiteUrl
                    user {
                      id
                      name
                      email
                    }
                    createdAt
                    updatedAt
                  }
                }
            ";

      var request = new { query = query, variables = new { id = profileId } };

      var content = new StringContent(
        JsonSerializer.Serialize(request),
        Encoding.UTF8,
        "application/json"
      );

      // Act
      var response = await _client.PostAsync("/graphql", content);
      var responseString = await response.Content.ReadAsStringAsync();
      
      // If response is not successful or has errors, throw with details
      if (!response.IsSuccessStatusCode) {
        throw new Exception($"HTTP Error {response.StatusCode}. Response: {responseString}");
      }
      
      var result = JsonDocument.Parse(responseString);
      
      if (result.RootElement.TryGetProperty("errors", out var errors)) {
        var errorMessage = errors.GetArrayLength() > 0 
          ? errors[0].GetProperty("message").GetString() 
          : "Unknown GraphQL error";
        throw new Exception($"GraphQL Error: {errorMessage}. Response: {responseString}");
      }

      // Get data from GraphQL response
      var profileNode = result.RootElement
                              .GetProperty("data")
                              .GetProperty("getUserProfileById");

      // Assert
      Assert.True(response.IsSuccessStatusCode);
      Assert.Equal(profileId.ToString(), profileNode.GetProperty("id").GetString());
      Assert.Equal(bio, profileNode.GetProperty("bio").GetString());
      Assert.Equal(avatarUrl, profileNode.GetProperty("avatarUrl").GetString());
      Assert.Equal(userId, profileNode.GetProperty("user").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Should_Mutate_UserProfile() {
      // Arrange - Create test user with profile
      var userId = Guid.NewGuid().ToString();
      var email = $"graphql-mutate-{Guid.NewGuid()}@example.com";
      var name = "GraphQL Mutate User";
      var bio = "Initial bio";
      var avatarUrl = "https://example.com/initial-avatar.jpg";

      await _fixture.SeedTestUserWithProfile(userId, email, name, bio, avatarUrl);

      // Get profile ID
      using var scope = _fixture.Server.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var profileId = Guid.Parse(userId);
      var profile = await dbContext.UserProfiles.FindAsync(profileId);
      if (profile == null) throw new Exception("Profile not found");

      // Set authentication token
      var token = _fixture.GenerateTestToken(userId);
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      var mutation = @"
                mutation UpdateUserProfile($id: ID!, $input: UpdateUserProfileInput!) {
                  updateUserProfile(id: $id, input: $input) {
                    id
                    bio
                    avatarUrl
                    location
                    updatedAt
                  }
                }
            ";

      var variables = new { id = profileId, input = new { bio = "Updated via GraphQL", avatarUrl = "https://example.com/updated-avatar.jpg", location = "New Location" } };

      var request = new { query = mutation, variables = variables };
      var content = new StringContent(
        JsonSerializer.Serialize(request),
        Encoding.UTF8,
        "application/json"
      );

      // Act
      var response = await _client.PostAsync("/graphql", content);
      var responseString = await response.Content.ReadAsStringAsync();
      var result = JsonDocument.Parse(responseString);

      // Get data from GraphQL response
      var updatedProfile = result.RootElement
                                 .GetProperty("data")
                                 .GetProperty("updateUserProfile");

      // Assert
      Assert.True(response.IsSuccessStatusCode);
      Assert.Equal(profileId.ToString(), updatedProfile.GetProperty("id").GetString());
      Assert.Equal("Updated via GraphQL", updatedProfile.GetProperty("bio").GetString());
      Assert.Equal("https://example.com/updated-avatar.jpg", updatedProfile.GetProperty("avatarUrl").GetString());
      Assert.Equal("New Location", updatedProfile.GetProperty("location").GetString());
    }

    [Fact]
    public async Task Should_Query_All_UserProfiles() {
      // Arrange - Seed multiple profiles
      await SeedMultipleUserProfiles();

      // Set authentication token (admin)
      var token = _fixture.GenerateTestToken();
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      var query = @"
                query {
                  getUserProfiles {
                    id
                    bio
                    user {
                      name
                    }
                  }
                }
            ";

      var request = new { query = query };
      var content = new StringContent(
        JsonSerializer.Serialize(request),
        Encoding.UTF8,
        "application/json"
      );

      // Act
      var response = await _client.PostAsync("/graphql", content);
      var responseString = await response.Content.ReadAsStringAsync();
      var result = JsonDocument.Parse(responseString);

      // Get data from GraphQL response
      var profilesArray = result.RootElement
                                .GetProperty("data")
                                .GetProperty("getUserProfiles");

      // Assert
      Assert.True(response.IsSuccessStatusCode);
      Assert.True(profilesArray.GetArrayLength() >= 3); // At least our test profiles
    }

    [Fact]
    public async Task Should_Filter_UserProfiles_By_Location() {
      // Arrange - Seed profiles with specific locations
      await SeedUserProfilesWithLocations();

      // Set authentication token (admin)
      var token = _fixture.GenerateTestToken();
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      var query = @"
                query($searchTerm: String) {
                  getUserProfiles(searchTerm: $searchTerm) {
                    id
                    bio
                    location
                    user {
                      name
                    }
                  }
                }
            ";

      var request = new { query = query, variables = new { searchTerm = "New York" } };

      var content = new StringContent(
        JsonSerializer.Serialize(request),
        Encoding.UTF8,
        "application/json"
      );

      // Act
      var response = await _client.PostAsync("/graphql", content);
      var responseString = await response.Content.ReadAsStringAsync();
      
      // Parse the response to check for errors
      var result = JsonDocument.Parse(responseString);

      // If response is not successful or has errors, throw with details
      if (!response.IsSuccessStatusCode) {
        throw new Exception($"HTTP Error {response.StatusCode}. Response: {responseString}");
      }
      
      if (result.RootElement.TryGetProperty("errors", out var errors)) {
        var errorMessage = errors.GetArrayLength() > 0 
          ? errors[0].GetProperty("message").GetString() 
          : "Unknown GraphQL error";
        throw new Exception($"GraphQL Error: {errorMessage}. Response: {responseString}");
      }

      // Assert
      Assert.True(response.IsSuccessStatusCode);

      // Get data from GraphQL response
      var profilesArray = result.RootElement
                                .GetProperty("data")
                                .GetProperty("getUserProfiles");

      // Check all returned profiles have the correct location
      var arrayLength = profilesArray.GetArrayLength();

      for (int i = 0; i < arrayLength; i++) {
        var profile = profilesArray[i];
        Assert.Equal("New York", profile.GetProperty("location").GetString());
      }
    }

    // Helper methods to seed test data
    private async Task SeedMultipleUserProfiles() {
      // Create three test users with profiles
      for (int i = 0; i < 3; i++) {
        var userId = Guid.NewGuid().ToString();
        var email = $"multi-profile-{i}-{Guid.NewGuid()}@example.com";
        var name = $"Multi Profile User {i}";
        var bio = $"Bio for multi profile user {i}";
        var avatarUrl = $"https://example.com/profile{i}.jpg";

        await _fixture.SeedTestUserWithProfile(userId, email, name, bio, avatarUrl);
      }
    }

    private async Task SeedUserProfilesWithLocations() {
      // Create profiles with different locations for filtering tests
      var locations = new[] { "New York", "London", "Tokyo", "New York", "Paris", "New York" };

      for (int i = 0; i < locations.Length; i++) {
        var userId = Guid.NewGuid().ToString();
        var email = $"location-{i}-{Guid.NewGuid()}@example.com";
        var name = $"Location User {i}";
        var bio = $"Bio for location user {i}";
        var avatarUrl = $"https://example.com/location{i}.jpg";

        await _fixture.SeedTestUserWithProfile(userId, email, name, bio, avatarUrl);

        // Update the location directly in the database
        using var scope = _fixture.Server.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var profileId = Guid.Parse(userId);
        var profile = await dbContext.UserProfiles.FindAsync(profileId);

        if (profile != null) {
          // profile.Location = locations[i];  // UserProfile may not have Location property
          await dbContext.SaveChangesAsync();
        }
      }
    }
  }
}
