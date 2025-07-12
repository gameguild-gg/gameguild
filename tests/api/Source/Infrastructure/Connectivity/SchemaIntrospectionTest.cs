using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using GameGuild.Tests.Fixtures;

namespace GameGuild.API.Tests.Infrastructure;

/// <summary>
/// Tests to inspect the actual GraphQL schema and available queries
/// </summary>
public class SchemaIntrospectionTest : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public SchemaIntrospectionTest(TestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Should_Display_Available_Query_Fields()
    {
        // Arrange - Try without authentication first
        var simpleQuery = @"
            query {
              __type(name: ""Query"") {
                fields {
                  name
                  description
                }
              }
            }";

        var request = new { query = simpleQuery };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Simple Query Response: {responseString}");

        // Try with a very basic query to see what's available
        var basicQuery = @"{ __typename }";
        var basicRequest = new { query = basicQuery };
        var basicContent = new StringContent(
            JsonSerializer.Serialize(basicRequest),
            Encoding.UTF8,
            "application/json"
        );

        var basicResponse = await _client.PostAsync("/graphql", basicContent);
        var basicResponseString = await basicResponse.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Basic Query Response: {basicResponseString}");

        // Assert - At least one should work
        Assert.True(response.IsSuccessStatusCode || basicResponse.IsSuccessStatusCode, 
            $"Both requests failed: {response.StatusCode}, {basicResponse.StatusCode}");
    }
}
