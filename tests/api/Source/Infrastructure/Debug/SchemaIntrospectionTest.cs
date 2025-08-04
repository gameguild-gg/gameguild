using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Debug;

/// <summary>
/// Debug test to introspect the GraphQL schema and see what's available
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
    public async Task Debug_GraphQL_Schema_Available_Fields()
    {
        // Introspection query to see what's in the schema
        var query = @"
            query IntrospectionQuery {
              __schema {
                queryType {
                  name
                  fields {
                    name
                    type {
                      name
                      kind
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

        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"Introspection Response Status: {response.StatusCode}");
        _output.WriteLine($"Introspection Response: {responseString}");

        // Just log the results, don't assert
        Assert.True(true, "Introspection complete");
    }
}
