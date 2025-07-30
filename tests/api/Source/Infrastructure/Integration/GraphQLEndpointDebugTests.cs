using System.Text;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Integration;

/// <summary>
/// Debug tests to understand GraphQL endpoint issues
/// </summary>
public class GraphQLEndpointDebugTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public GraphQLEndpointDebugTests(TestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Debug_GraphQL_Endpoint_Response()
    {
        // Try GET request to see what happens
        var getResponse = await _client.GetAsync("/graphql");
        _output.WriteLine($"GET /graphql - Status: {getResponse.StatusCode}");
        _output.WriteLine($"GET /graphql - Headers: {string.Join(", ", getResponse.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
        
        if (getResponse.Headers.Location != null)
        {
            _output.WriteLine($"GET /graphql - Redirect Location: {getResponse.Headers.Location}");
        }
        
        var getContent = await getResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"GET /graphql - Content: {getContent}");

        // Try POST request with basic query
        var query = @"{ __typename }";
        var request = new { query = query };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var postResponse = await _client.PostAsync("/graphql", content);
        _output.WriteLine($"POST /graphql - Status: {postResponse.StatusCode}");
        
        if (postResponse.Headers.Location != null)
        {
            _output.WriteLine($"POST /graphql - Redirect Location: {postResponse.Headers.Location}");
        }
        
        var postContent = await postResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"POST /graphql - Content: {postContent}");
    }

    [Fact]
    public void Debug_Service_Registration()
    {
        using var scope = _fixture.Server.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        // Check if GraphQL services are registered
        var graphqlServices = services.GetServices<object>()
            .Where(s => s.GetType().FullName?.Contains("GraphQL", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
            
        _output.WriteLine($"GraphQL services found: {graphqlServices.Count}");
        foreach (var service in graphqlServices.Take(10))
        {
            _output.WriteLine($"  - {service.GetType().FullName}");
        }
    }
}
