using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Connectivity;

/// <summary>
/// Test to verify GraphQL server is working at all
/// </summary>
public class GraphQLServerValidationTest : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public GraphQLServerValidationTest(TestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task GraphQL_Server_Should_Respond_To_Basic_Query()
    {
        // Start with the most basic GraphQL query possible
        var basicQuery = @"{ __typename }";
        
        var request = new { query = basicQuery };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        // This should work regardless of schema contents
        Assert.True(response.IsSuccessStatusCode, 
            $"Basic GraphQL query failed. Status: {response.StatusCode}, Response: {responseString}");
            
        // Should contain "Query" as the root type name
        Assert.Contains("Query", responseString);
    }

    [Fact]
    public async Task GraphQL_Endpoint_Should_Exist()
    {
        // Test if /graphql endpoint exists with a GET request
        var response = await _client.GetAsync("/graphql");
        
        // GraphQL typically returns 400 for GET requests without query params, but should not be 404
        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
