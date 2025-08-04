using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Debug;

/// <summary>
/// Simple test to verify basic GraphQL functionality
/// </summary>
public class GraphQLHealthTest : IClassFixture<MockModuleTestServerFixture>
{
    private readonly MockModuleTestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public GraphQLHealthTest(MockModuleTestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task GraphQL_Health_Endpoint_Should_Work()
    {
        // Test the basic health query from the base Query class
        var query = @"
            query {
              health
            }";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Health Response Status: {response.StatusCode}");
        _output.WriteLine($"Health Response: {responseString}");

        Assert.True(response.IsSuccessStatusCode, $"GraphQL health request failed with status: {response.StatusCode}");
        
        var result = JsonDocument.Parse(responseString);
        
        // Verify no errors
        Assert.False(result.RootElement.TryGetProperty("errors", out _), "GraphQL returned errors");
        
        // Verify data exists
        Assert.True(result.RootElement.TryGetProperty("data", out var data), "GraphQL returned no data");
        
        // Verify health field exists
        Assert.True(data.TryGetProperty("health", out var health), "GraphQL health field missing");
        
        // Verify health value
        Assert.Equal("GraphQL API is healthy", health.GetString());
    }
}
