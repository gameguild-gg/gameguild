using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
// using GameGuild.API.Models;
using GameGuild.Tests.Modules.Fixtures;

namespace GameGuild.Tests.Modules.User.E2E.GraphQL
{
    public class UserGraphQLTests : IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _fixture;
        private readonly HttpClient _client;

        public UserGraphQLTests(TestServerFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.CreateClient();
        }

        [Fact]
        public async Task Should_Query_User_Profile()
        {
            // Arrange - Create and seed test user with profile
            var userId = Guid.NewGuid().ToString();
            var email = $"graphql-user-{Guid.NewGuid()}@example.com";
            var name = "GraphQL Test User";
            var bio = "GraphQL test bio";
            var avatarUrl = "https://example.com/graphql-avatar.jpg";
            
            await _fixture.SeedTestUserWithProfile(userId, email, name, bio, avatarUrl);
            
            // Set authentication token
            var token = _fixture.GenerateTestToken(userId);
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var query = @"
                query GetUserProfile($id: ID!) {
                  user(id: $id) {
                    id
                    name
                    email
                    createdAt
                    updatedAt
                    profile {
                      bio
                      avatarUrl
                    }
                  }
                }
            ";
            
            var request = new 
            {
                query = query,
                variables = new { id = userId }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseString);
            
            // Get data from GraphQL response
            var userNode = result.RootElement
                .GetProperty("data")
                .GetProperty("user");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(userId, userNode.GetProperty("id").GetString());
            Assert.Equal(name, userNode.GetProperty("name").GetString());
            Assert.Equal(email, userNode.GetProperty("email").GetString());
            Assert.NotNull(userNode.GetProperty("createdAt").GetString());
            Assert.Equal(bio, userNode.GetProperty("profile").GetProperty("bio").GetString());
            Assert.Equal(avatarUrl, userNode.GetProperty("profile").GetProperty("avatarUrl").GetString());
        }
        
        [Fact]
        public async Task Should_Query_Multiple_Users()
        {
            // Arrange - Seed multiple test users
            await SeedMultipleTestUsers();
            
            // Set authentication token for admin access
            var token = _fixture.GenerateTestToken();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var query = @"
                query {
                  users {
                    id
                    name
                    email
                  }
                }
            ";
            
            var request = new { query = query };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/graphql", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseString);
            
            // Get data from GraphQL response
            var usersArray = result.RootElement
                .GetProperty("data")
                .GetProperty("users");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(usersArray.GetArrayLength() >= 3); // At least our 3 test users
        }

        // Helper method to seed multiple test users
        private async Task SeedMultipleTestUsers()
        {
            // Create three test users
            for (int i = 0; i < 3; i++)
            {
                var userId = Guid.NewGuid().ToString();
                var email = $"multi-user-{i}-{Guid.NewGuid()}@example.com";
                var name = $"Multi User {i}";
                var bio = $"Bio for multi user {i}";
                var avatarUrl = $"https://example.com/avatar{i}.jpg";
                
                await _fixture.SeedTestUserWithProfile(userId, email, name, bio, avatarUrl);
            }
        }
    }
}
