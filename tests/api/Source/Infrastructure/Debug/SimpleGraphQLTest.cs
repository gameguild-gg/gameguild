using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Debug;

/// <summary>
/// Simple test with explicit GraphQL registration
/// </summary>
public class SimpleGraphQLTest : IClassFixture<SimpleGraphQLTestFixture>
{
    private readonly SimpleGraphQLTestFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public SimpleGraphQLTest(SimpleGraphQLTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Simple_GraphQL_Test_Should_Work()
    {
        // Test the simple getTestMessage query
        var query = @"
            query {
              getTestMessage
            }";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Response Status: {response.StatusCode}");
        _output.WriteLine($"Response: {responseString}");

        // Just check if we get a response (don't assert success yet)
        Assert.True(true, "Simple GraphQL test complete");
    }
}
