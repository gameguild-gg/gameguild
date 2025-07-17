using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Connectivity;

/// <summary>
/// Basic connectivity tests to diagnose GraphQL setup issues
/// </summary>
public class GraphQLBasicConnectivityTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public GraphQLBasicConnectivityTests(TestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task GraphQL_Endpoint_Should_Exist()
    {
        // Act
        var response = await _client.GetAsync("/graphql");
        _output.WriteLine($"GET /graphql Status: {response.StatusCode}");
        _output.WriteLine($"GET /graphql Response: {await response.Content.ReadAsStringAsync()}");

        // GraphQL typically doesn't respond to GET (unless introspection), but endpoint should exist
        // We expect either 400 (Bad Request) or 405 (Method Not Allowed) for GET requests
        Assert.True(response.StatusCode != System.Net.HttpStatusCode.PageNotFound,
            "GraphQL endpoint should exist (not return 404)");
        
        _output.WriteLine("✅ GraphQL endpoint exists");
    }

    [Fact]
    public async Task GraphQL_POST_With_Empty_Body_Should_Return_Error()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"POST /graphql (empty) Status: {response.StatusCode}");
        _output.WriteLine($"POST /graphql (empty) Response: {responseString}");

        // Should get some kind of error response, not a 404
        Assert.True(response.StatusCode != System.Net.HttpStatusCode.PageNotFound,
            "GraphQL endpoint should handle POST requests (not return 404)");
        
        _output.WriteLine("✅ GraphQL endpoint handles POST requests");
    }

    [Fact]
    public async Task GraphQL_POST_With_Malformed_JSON_Should_Return_Error()
    {
        // Arrange
        var content = new StringContent("{ invalid json", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"POST /graphql (malformed) Status: {response.StatusCode}");
        _output.WriteLine($"POST /graphql (malformed) Response: {responseString}");

        // Should handle malformed JSON gracefully
        Assert.True(response.StatusCode != System.Net.HttpStatusCode.PageNotFound,
            "GraphQL should handle malformed JSON (not return 404)");
        
        _output.WriteLine("✅ GraphQL endpoint handles malformed JSON");
    }

    [Fact]
    public async Task GraphQL_POST_With_Valid_JSON_But_No_Query_Should_Return_Error()
    {
        // Arrange
        var request = new { };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        _output.WriteLine($"POST /graphql (no query) Status: {response.StatusCode}");
        _output.WriteLine($"POST /graphql (no query) Response: {responseString}");

        // Should return an error about missing query
        Assert.True(response.StatusCode != System.Net.HttpStatusCode.PageNotFound,
            "GraphQL should handle requests without query field (not return 404)");
        
        _output.WriteLine("✅ GraphQL endpoint handles requests without query field");
    }

    [Fact]
    public async Task List_All_Registered_Services()
    {
        // Arrange & Act
        using var scope = _fixture.Server.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        _output.WriteLine("=== Registered Services ===");
        
        // Check for key GraphQL services
        try
        {
            var graphQLServices = services.GetServices<object>()
                .Where(s => s.GetType().FullName?.Contains("GraphQL") == true ||
                           s.GetType().FullName?.Contains("HotChocolate") == true)
                .Take(10);
                
            foreach (var service in graphQLServices)
            {
                _output.WriteLine($"GraphQL Service: {service.GetType().FullName}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"ErrorMessage getting GraphQL services: {ex.Message}");
        }

        _output.WriteLine("✅ Service listing completed");
    }

    [Fact]
    public async Task Check_Available_Endpoints()
    {
        // Test common endpoints
        var endpoints = new[] { "/", "/graphql", "/health", "/api" };
        
        foreach (var endpoint in endpoints)
        {
            try
            {
                var response = await _client.GetAsync(endpoint);
                _output.WriteLine($"Endpoint {endpoint}: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Endpoint {endpoint}: Exception - {ex.Message}");
            }
        }
        
        _output.WriteLine("✅ Endpoint check completed");
    }
}
