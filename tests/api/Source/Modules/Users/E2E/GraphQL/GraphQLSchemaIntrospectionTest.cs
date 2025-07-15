using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;


namespace GameGuild.Tests.Modules.Users.E2E.GraphQL {
  public class GraphQLSchemaIntrospectionTest : IClassFixture<TestServerFixture> {
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;

    public GraphQLSchemaIntrospectionTest(TestServerFixture fixture) {
      _fixture = fixture;
      _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Should_Show_Available_Query_Fields() {
      // Arrange
      var token = _fixture.GenerateTestToken();
      _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      var query = @"
                query IntrospectSchema {
                  __schema {
                    queryType {
                      fields {
                        name
                        type {
                          name
                        }
                      }
                    }
                  }
                }";

      var request = new { query = query };

      var content = new StringContent(
        JsonSerializer.Serialize(request),
        Encoding.UTF8,
        "application/json"
      );

      // Act
      var response = await _client.PostAsync("/graphql", content);
      var responseString = await response.Content.ReadAsStringAsync();
      
      // Debug: Log the schema introspection
      Console.WriteLine($"GraphQL Schema Introspection Response: {responseString}");
      
      // Assert
      Assert.True(response.IsSuccessStatusCode, $"GraphQL introspection failed. Status: {response.StatusCode}");
      
      var result = JsonDocument.Parse(responseString);
      Assert.True(result.RootElement.TryGetProperty("data", out _), "Response should contain 'data' field");
    }
  }
}
