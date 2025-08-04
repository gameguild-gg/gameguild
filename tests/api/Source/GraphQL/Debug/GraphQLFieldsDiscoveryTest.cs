using System.Net.Http.Json;
using GameGuild.Tests.Fixtures;


namespace GameGuild.Tests.GraphQL.Debug {
  public class GraphQLFieldsDiscoveryTest : IClassFixture<TestServerFixture> {
    private readonly TestServerFixture _fixture;

    public GraphQLFieldsDiscoveryTest(TestServerFixture fixture) {
      _fixture = fixture;
    }

    [Fact]
    public async Task Should_Discover_Available_GraphQL_Fields() {
      // Act - Try to run a simple query that should work
      var query = @"
        query {
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

      var requestBody = new {
        query = query,
      };

      var response = await _fixture.Server.CreateClient().PostAsJsonAsync("/graphql", requestBody);
      var content = await response.Content.ReadAsStringAsync();
      
      Console.WriteLine($"GraphQL Schema Query Response: {content}");
      Console.WriteLine($"Response Status: {response.StatusCode}");

      // This test is for debugging - we just want to see the output
      Assert.True(true, "This is a discovery test");
    }
  }
}
