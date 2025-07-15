using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Integration;

/// <summary>
/// Debug tests to understand GraphQL endpoint issues with TestServerFixture
/// </summary>
public class GraphQLDebugTestServerTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public GraphQLDebugTestServerTests(TestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Debug_GraphQL_Status_Codes()
    {
        // Test GET request to see what happens
        var getResponse = await _client.GetAsync("/graphql");
        _output.WriteLine($"GET /graphql - Status: {getResponse.StatusCode} ({(int)getResponse.StatusCode})");
        _output.WriteLine($"GET /graphql - IsSuccessStatusCode: {getResponse.IsSuccessStatusCode}");
        
        if (getResponse.Headers.Location != null)
        {
            _output.WriteLine($"GET /graphql - Redirect Location: {getResponse.Headers.Location}");
        }
        
        var getContent = await getResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /graphql - Content: {getContent}");

        // Test POST request with invalid query
        var invalidQuery = @"{ invalidField }";
        var request = new { query = invalidQuery };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var postResponse = await _client.PostAsync("/graphql", content);
        _output.WriteLine($"POST /graphql (invalid) - Status: {postResponse.StatusCode} ({(int)postResponse.StatusCode})");
        _output.WriteLine($"POST /graphql (invalid) - IsSuccessStatusCode: {postResponse.IsSuccessStatusCode}");
        
        var postContent = await postResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /graphql (invalid) - Content: {postContent}");

        // Test POST request with valid query
        var validQuery = @"{ __typename }";
        var validRequest = new { query = validQuery };
        var validContent = new StringContent(
            JsonSerializer.Serialize(validRequest),
            Encoding.UTF8,
            "application/json"
        );

        var validResponse = await _client.PostAsync("/graphql", validContent);
        _output.WriteLine($"POST /graphql (valid) - Status: {validResponse.StatusCode} ({(int)validResponse.StatusCode})");
        _output.WriteLine($"POST /graphql (valid) - IsSuccessStatusCode: {validResponse.IsSuccessStatusCode}");
        
        var validResponseContent = await validResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /graphql (valid) - Content: {validResponseContent}");
    }
}
