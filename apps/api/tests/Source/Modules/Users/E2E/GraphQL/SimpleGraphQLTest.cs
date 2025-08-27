using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;


namespace GameGuild.Tests.Modules.Users.E2E.GraphQL {
  public class SimpleGraphQLTest : IClassFixture<TestServerFixture> {
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;

    public SimpleGraphQLTest(TestServerFixture fixture) {
      _fixture = fixture;
      _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Should_Connect_To_GraphQL_Endpoint() {
      // Test basic GraphQL connectivity with a simple query
      var query = @"{ __typename }";
      var request = new { query };
      var content = new StringContent(
        JsonSerializer.Serialize(request),
        Encoding.UTF8,
        "application/json"
      );

      var response = await _client.PostAsync("/graphql", content);
      var responseString = await response.Content.ReadAsStringAsync();

      Console.WriteLine($"Response Status: {response.StatusCode}");
      Console.WriteLine($"Response Content: {responseString}");

      Assert.True(response.IsSuccessStatusCode, $"GraphQL endpoint failed. Status: {response.StatusCode}, Content: {responseString}");
    }
  }
}
