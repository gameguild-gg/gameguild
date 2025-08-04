using System.Net.Http.Json;
using System.Text.Json;
using GameGuild.Tests.Fixtures;


namespace GameGuild.Tests.GraphQL.Debug {
  public class GraphQLBasicTest : IClassFixture<TestServerFixture> {
    private readonly TestServerFixture _fixture;

    public GraphQLBasicTest(TestServerFixture fixture) {
      _fixture = fixture;
    }

    [Fact]
    public async Task Should_Return_GraphQL_Error_For_Invalid_Query() {
      // Test with an obviously wrong query to confirm GraphQL is working
      var query = @"invalid query syntax {";

      var requestBody = new {
        query = query,
      };

      var response = await _fixture.Server.CreateClient().PostAsJsonAsync("/graphql", requestBody);
      var content = await response.Content.ReadAsStringAsync();
      
      Console.WriteLine($"Invalid Query Response: {content}");
      Console.WriteLine($"Response Status: {response.StatusCode}");

      // Should get a GraphQL syntax error, not a 404 or server error
      Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
      Assert.Contains("error", content.ToLower());
    }

    [Fact]
    public async Task Should_Handle_Empty_Query() {
      // Test with empty query
      var query = "";

      var requestBody = new {
        query = query,
      };

      var response = await _fixture.Server.CreateClient().PostAsJsonAsync("/graphql", requestBody);
      var content = await response.Content.ReadAsStringAsync();
      
      Console.WriteLine($"Empty Query Response: {content}");
      Console.WriteLine($"Response Status: {response.StatusCode}");

      // Should get a GraphQL error about missing query
      Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_Have_Query_Type_But_No_Fields() {
      // Test if Query type exists but has no fields
      var query = @"
        query {
          nonExistentField
        }";

      var requestBody = new {
        query = query,
      };

      var response = await _fixture.Server.CreateClient().PostAsJsonAsync("/graphql", requestBody);
      var content = await response.Content.ReadAsStringAsync();
      
      Console.WriteLine($"Non-Existent Field Response: {content}");
      Console.WriteLine($"Response Status: {response.StatusCode}");

      var jsonResponse = JsonDocument.Parse(content);
      
      if (jsonResponse.RootElement.TryGetProperty("errors", out var errors)) {
        var firstError = errors.EnumerateArray().First();
        var message = firstError.GetProperty("message").GetString();
        Console.WriteLine($"Error message: {message}");
        
        // Should mention that the field doesn't exist on Query type
        Assert.Contains("does not exist", message!);
        Assert.Contains("Query", message!);
      }
    }
  }
}
