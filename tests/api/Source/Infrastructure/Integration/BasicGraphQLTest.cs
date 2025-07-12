using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GameGuild.API.Tests.Infrastructure.Integration;

public class BasicGraphQLTest : IClassFixture<MockModuleTestServerFixture>
{
    private readonly MockModuleTestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public BasicGraphQLTest(MockModuleTestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Basic_GraphQL_Health_Query_Should_Work()
    {
        // Test the basic health query from the root Query type
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
        
        _output.WriteLine($"Health Query Response Status: {response.StatusCode}");
        _output.WriteLine($"Health Query Response: {responseString}");

        Assert.True(response.IsSuccessStatusCode, $"GraphQL request failed with status: {response.StatusCode}");
        
        var result = JsonDocument.Parse(responseString);
        
        // Verify no errors
        if (result.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessages = errors.EnumerateArray().Select(e => e.GetProperty("message").GetString());
            Assert.True(false, $"GraphQL errors: {string.Join(", ", errorMessages)}");
        }

        // Verify the health query works
        var data = result.RootElement.GetProperty("data");
        var message = data.GetProperty("health").GetString();
        
        Assert.Equal("GraphQL API is healthy", message);
        _output.WriteLine("✅ Basic GraphQL health query working");
    }
}
