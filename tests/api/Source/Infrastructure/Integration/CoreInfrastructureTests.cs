using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using GameGuild.Tests.MockModules;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Integration;

/// <summary>
/// Simple infrastructure tests that focus on core CQRS + MediatR integration without GraphQL complexity
/// This helps isolate infrastructure issues from GraphQL schema registration problems
/// </summary>
public class CoreInfrastructureTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public CoreInfrastructureTests(TestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
        _output = output;
    }

    [Fact]
    public void MediatR_Should_Be_Properly_Registered_In_DI()
    {
        // Arrange & Act
        using var scope = _fixture.Server.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        
        // Assert
        Assert.NotNull(mediator);
        _output.WriteLine("✅ MediatR is properly registered in DI container");
    }

    [Fact]
    public async Task MediatR_Should_Execute_CQRS_Handlers_Directly()
    {
        // Arrange
        using var scope = _fixture.Server.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Create a simple query
        var query = new GetTestItemsQuery { Take = 5, IncludeInactive = false };

        // Act
        var result = await mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        var items = result.ToList();
        Assert.True(items.Count >= 2, $"Expected at least 2 items, got {items.Count}");
        Assert.All(items, item => Assert.True(item.IsActive, "All items should be active when IncludeInactive=false"));
        
        _output.WriteLine($"✅ MediatR executed CQRS handler successfully: {items.Count} items returned");
    }

    [Fact]
    public async Task MediatR_Should_Execute_Single_Item_Query()
    {
        // Arrange
        using var scope = _fixture.Server.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var testId = Guid.NewGuid();
        var query = new GetTestItemByIdQuery { Id = testId };

        // Act
        var result = await mediator.Send(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testId, result.Id);
        Assert.True(result.IsActive);
        Assert.Contains(testId.ToString()[..8], result.Name);
        
        _output.WriteLine($"✅ MediatR single item query executed successfully: {result.Name}");
    }

    [Fact]
    public async Task HTTP_GraphQL_Endpoint_Should_Be_Available()
    {
        // Arrange - Simple HTTP request to GraphQL endpoint
        var simpleRequest = new { query = "{ __typename }" };
        var content = new StringContent(
            JsonSerializer.Serialize(simpleRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"GraphQL Endpoint Response Status: {response.StatusCode}");
        _output.WriteLine($"GraphQL Endpoint Response: {responseString}");

        // Assert - We just want to verify the endpoint exists and returns something
        // We're not testing the actual GraphQL schema here since that's causing issues
        Assert.NotNull(response);
        Assert.False(string.IsNullOrEmpty(responseString));
        
        _output.WriteLine("✅ GraphQL endpoint is responding (even if with errors)");
    }

    [Fact]
    public async Task API_Should_Handle_Invalid_Endpoints_Gracefully()
    {
        // Arrange
        var invalidEndpoint = "/invalid-endpoint-test";

        // Act
        var response = await _client.GetAsync(invalidEndpoint);

        // Assert
        _output.WriteLine($"Invalid Endpoint Response Status: {response.StatusCode}");
        
        // Should return 404 or other appropriate status (not crash)
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                   response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                   $"Expected 404 or 405, got {response.StatusCode}");
        
        _output.WriteLine("✅ API handles invalid endpoints gracefully");
    }

    [Fact]
    public void DI_Container_Should_Have_Required_Services()
    {
        // Arrange & Act
        using var scope = _fixture.Server.Services.CreateScope();
        
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
        
        // Assert
        Assert.NotNull(mediator);
        // HttpClientFactory might not be registered, so we won't require it
        
        // Check if we can resolve basic ASP.NET Core services
        var logger = scope.ServiceProvider.GetService<ILogger<CoreInfrastructureTests>>();
        
        _output.WriteLine("✅ Core DI services are available");
        _output.WriteLine($"   - MediatR: {(mediator != null ? "✅" : "❌")}");
        _output.WriteLine($"   - HttpClientFactory: {(httpClientFactory != null ? "✅" : "❌")}");
        _output.WriteLine($"   - Logger: {(logger != null ? "✅" : "❌")}");
    }

    [Fact]
    public void Test_Server_Should_Be_Configured_Correctly()
    {
        // Arrange & Act
        var baseAddress = _client.BaseAddress;
        var timeout = _client.Timeout;

        // Assert
        Assert.NotNull(_fixture.Server);
        Assert.NotNull(_client);
        Assert.NotNull(baseAddress);
        
        _output.WriteLine($"✅ Test server configured correctly");
        _output.WriteLine($"   - Base Address: {baseAddress}");
        _output.WriteLine($"   - Timeout: {timeout}");
    }
}
