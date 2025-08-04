using System.Net.Http.Json;
using GameGuild.Tests.Fixtures;


namespace GameGuild.Tests.GraphQL.Debug {
  public class GraphQLServerTest : IClassFixture<TestServerFixture> {
    private readonly TestServerFixture _fixture;

    public GraphQLServerTest(TestServerFixture fixture) {
      _fixture = fixture;
    }

    [Fact]
    public async Task Should_Have_Basic_GraphQL_Endpoint() {
      // Test with a basic query that should work
      var query = @"
        query {
          __type(name: ""Query"") {
            name
          }
        }";

      var requestBody = new {
        query = query,
      };

      var response = await _fixture.Server.CreateClient().PostAsJsonAsync("/graphql", requestBody);
      var content = await response.Content.ReadAsStringAsync();
      
      Console.WriteLine($"Basic GraphQL Response: {content}");
      Console.WriteLine($"Response Status: {response.StatusCode}");

      // Just check that we get some response
      Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Have_Query_Type_Available() {
      // Test what query fields are available without introspection
      var query = @"
        query {
          unknownField
        }";

      var requestBody = new {
        query = query,
      };

      var response = await _fixture.Server.CreateClient().PostAsJsonAsync("/graphql", requestBody);
      var content = await response.Content.ReadAsStringAsync();
      
      Console.WriteLine($"Unknown Field Response: {content}");
      Console.WriteLine($"Response Status: {response.StatusCode}");

      // The response should mention available fields if unknownField doesn't exist
      Assert.Contains("field", content.ToLower());
    }
  }
}
