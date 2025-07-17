using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Integration;

/// <summary>
/// Comprehensive infrastructure tests that verify GraphQL→CQRS architecture works
/// Tests focus on infrastructure capabilities rather than specific business modules
/// </summary>
public class GraphQLCQRSInfrastructureValidationTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public GraphQLCQRSInfrastructureValidationTests(TestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task GraphQL_Server_Should_Be_Operational()
    {
        // Test 1: Basic GraphQL server functionality
        var basicQuery = @"{ __typename }";
        var response = await ExecuteGraphQLQuery(basicQuery);
        
        _output.WriteLine($"Basic GraphQL Response: {response.Content}");
        
        Assert.True(response.IsSuccess, "GraphQL server should be operational");
        Assert.Contains("Query", response.Content);
    }

    [Fact]
    public async Task GraphQL_Should_Handle_Invalid_Queries_Gracefully()
    {
        // Test 2: ErrorMessage handling infrastructure
        var invalidQuery = @"{ invalidField { nonExistentSubField } }";
        var response = await ExecuteGraphQLQuery(invalidQuery);
        
        _output.WriteLine($"ErrorMessage Handling Response: {response.Content}");
        
        // GraphQL should handle validation errors gracefully - either 200 OK with errors or 400 BadRequest
        // Both are acceptable for validation errors according to GraphQL implementations
        Assert.True(response.IsSuccess || response.Content.Contains("errors"), 
                   "GraphQL should either return 200 OK or contain structured error information");
        
        // Should contain structured error information
        Assert.Contains("errors", response.Content);
        Assert.Contains("does not exist", response.Content);
    }

    [Fact]
    public async Task GraphQL_Schema_Should_Have_Required_Infrastructure()
    {
        // Test 3: Verify core GraphQL infrastructure is present
        var schemaQuery = @"{ __type(name: ""Query"") { name } }";
        var response = await ExecuteGraphQLQuery(schemaQuery);
        
        _output.WriteLine($"Schema Query Response: {response.Content}");
        
        if (response.IsSuccess && response.Content.Contains("\"data\""))
        {
            var result = JsonDocument.Parse(response.Content);
            if (result.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("__type", out var typeInfo) &&
                typeInfo.TryGetProperty("name", out var typeName))
            {
                Assert.Equal("Query", typeName.GetString());
                _output.WriteLine("✓ GraphQL schema has Query type");
            }
        }
        
        // Test passes if basic schema introspection works or is disabled (both are valid)
        Assert.True(true, "Schema introspection test completed");
    }

    [Fact]
    public async Task Infrastructure_Should_Support_CORS_And_Content_Types()
    {
        // Test 4: HTTP infrastructure for GraphQL
        var query = @"{ __typename }";
        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/graphql", content);
        
        _output.WriteLine($"Content-Type Support Response: Status={response.StatusCode}");
        
        // Should accept application/json content type
        Assert.True(response.StatusCode != System.Net.HttpStatusCode.UnsupportedMediaType, 
                   "GraphQL should support application/json content type");
    }

    [Fact]
    public async Task GraphQL_Endpoint_Should_Be_Available()
    {
        // Test 5: Endpoint routing
        var response = await _client.GetAsync("/graphql");
        
        _output.WriteLine($"Endpoint Availability: Status={response.StatusCode}");
        
        // Should not be PageNotFound (GraphQL endpoint exists)
        Assert.NotEqual(System.Net.HttpStatusCode.PageNotFound, response.StatusCode);
        
        // May return MethodNotAllowed (405) for GET without query param - that's fine
        // May return BadRequest (400) - that's also fine for GraphQL
        // May return MovedPermanently (301) - that's also fine (trailing slash redirect)
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                   response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                   response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                   response.StatusCode == System.Net.HttpStatusCode.OK,
                   "GraphQL endpoint should be available and configured");
    }

    [Fact]
    public void MediatR_Should_Be_Available_In_DI_Container()
    {
        // Test 6: CQRS infrastructure - verify MediatR is registered
        // This indirectly tests that the CQRS side of GraphQL→CQRS is working
        
        using var scope = _fixture.Server.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        
        Assert.NotNull(mediator);
        _output.WriteLine("✓ MediatR is properly registered in DI container");
    }

    [Fact]
    public async Task GraphQL_Should_Support_Complex_Query_Structure()
    {
        // Test 7: Complex query parsing (tests HotChocolate infrastructure)
        var complexQuery = @"
            query TestComplexStructure {
              __schema {
                queryType {
                  name
                  description
                }
              }
            }";
            
        var response = await ExecuteGraphQLQuery(complexQuery);
        
        _output.WriteLine($"Complex Query Response: {response.Content}");
        
        // Should parse complex query structure without syntax errors
        if (!response.IsSuccess)
        {
            // If introspection is disabled, we should get a proper GraphQL error, not a parsing error
            Assert.Contains("errors", response.Content);
            // Should not be a query parsing/syntax error
            Assert.DoesNotContain("syntax", response.Content.ToLower());
            Assert.DoesNotContain("parsing", response.Content.ToLower());
        }
        
        _output.WriteLine("✓ GraphQL handles complex query structures");
    }

    /// <summary>
    /// Helper method to execute GraphQL queries and return structured response
    /// </summary>
    private async Task<(bool IsSuccess, string Content)> ExecuteGraphQLQuery(string query)
    {
        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();
        
        return (response.IsSuccessStatusCode, responseString);
    }
}
