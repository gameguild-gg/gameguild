using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using GameGuild.Tests.MockModules;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Xunit.Abstractions;

namespace GameGuild.API.Tests.Infrastructure;

/// <summary>
/// Infrastructure tests using only mock modules to isolate GraphQL → CQRS → MediatR integration
/// This helps identify if issues are in infrastructure setup or business module registration
/// </summary>
public class MockModuleInfrastructureTests : IClassFixture<MockModuleTestServerFixture>
{
    private readonly MockModuleTestServerFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public MockModuleInfrastructureTests(MockModuleTestServerFixture fixture, ITestOutputHelper output)
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
    public void Mock_Handlers_Should_Be_Registered_In_DI()
    {
        // Arrange & Act
        using var scope = _fixture.Server.Services.CreateScope();
        var testItemsHandler = scope.ServiceProvider.GetService<IRequestHandler<GetTestItemsQuery, IEnumerable<TestItem>>>();
        var testItemByIdHandler = scope.ServiceProvider.GetService<IRequestHandler<GetTestItemByIdQuery, TestItem?>>();
        
        // Assert
        Assert.NotNull(testItemsHandler);
        Assert.NotNull(testItemByIdHandler);
        _output.WriteLine("✅ Mock handlers are properly registered in DI container");
    }

    [Fact]
    public async Task GraphQL_Endpoint_Should_Be_Available()
    {
        // Arrange
        var simpleQuery = @"
            query {
              getTestMessage
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

        _output.WriteLine($"GraphQL Endpoint Response Status: {response.StatusCode}");
        _output.WriteLine($"GraphQL Endpoint Response: {responseString}");

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"GraphQL endpoint failed with status: {response.StatusCode}");
        
        var result = JsonDocument.Parse(responseString);
        Assert.True(result.RootElement.TryGetProperty("data", out var data), "Response should contain data");
        Assert.True(data.TryGetProperty("getTestMessage", out var message), "Data should contain getTestMessage");
        Assert.Equal("Hello from GraphQL infrastructure test!", message.GetString());
        
        _output.WriteLine("✅ GraphQL endpoint is available and working");
    }

    [Fact]
    public async Task GraphQL_Should_Execute_CQRS_Query_Through_MediatR()
    {
        // Arrange
        var query = @"
            query {
              getTestItems(take: 5) {
                id
                name
                description
                createdAt
                isActive
              }
            }";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"CQRS Query Response Status: {response.StatusCode}");
        _output.WriteLine($"CQRS Query Response: {responseString}");

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"GraphQL CQRS query failed with status: {response.StatusCode}");
        
        var result = JsonDocument.Parse(responseString);
        
        // Verify no errors
        if (result.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessages = errors.EnumerateArray().Select(e => e.GetProperty("message").GetString());
            Assert.True(false, $"GraphQL errors: {string.Join(", ", errorMessages)}");
        }

        // Verify data structure
        Assert.True(result.RootElement.TryGetProperty("data", out var data), "Response should contain data");
        Assert.True(data.TryGetProperty("getTestItems", out var items), "Data should contain getTestItems");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        
        var itemCount = items.GetArrayLength();
        Assert.True(itemCount >= 2, $"Expected at least 2 test items, found {itemCount}");
        
        // Verify item structure
        var firstItem = items[0];
        Assert.True(firstItem.TryGetProperty("id", out _), "Item should have id");
        Assert.True(firstItem.TryGetProperty("name", out _), "Item should have name");
        Assert.True(firstItem.TryGetProperty("description", out _), "Item should have description");
        Assert.True(firstItem.TryGetProperty("isActive", out _), "Item should have isActive");
        
        _output.WriteLine($"✅ GraphQL → CQRS → MediatR integration working: {itemCount} items returned");
    }

    [Fact]
    public async Task GraphQL_Should_Execute_Single_Item_Query_Through_MediatR()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var query = $@"
            query {{
              getTestItemById(id: ""{testId}"") {{
                id
                name
                description
                isActive
              }}
            }}";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"Single Item Query Response Status: {response.StatusCode}");
        _output.WriteLine($"Single Item Query Response: {responseString}");

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"GraphQL single item query failed with status: {response.StatusCode}");
        
        var result = JsonDocument.Parse(responseString);
        
        // Verify no errors
        if (result.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessages = errors.EnumerateArray().Select(e => e.GetProperty("message").GetString());
            Assert.True(false, $"GraphQL errors: {string.Join(", ", errorMessages)}");
        }

        // Verify data structure
        Assert.True(result.RootElement.TryGetProperty("data", out var data), "Response should contain data");
        Assert.True(data.TryGetProperty("getTestItemById", out var item), "Data should contain getTestItemById");
        
        // Verify the item was found and has the correct ID
        Assert.NotEqual(JsonValueKind.Null, item.ValueKind);
        Assert.True(item.TryGetProperty("id", out var id), "Item should have id");
        Assert.Equal(testId.ToString(), id.GetString());
        
        _output.WriteLine($"✅ Single item query through CQRS working: Found item with ID {testId}");
    }

    [Fact]
    public async Task GraphQL_Should_Handle_Query_Parameters()
    {
        // Arrange - Test with different parameters
        var query = @"
            query {
              getTestItems(take: 2, includeInactive: true) {
                id
                name
                isActive
              }
            }";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"Parameterized Query Response: {responseString}");

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Parameterized query failed with status: {response.StatusCode}");
        
        var result = JsonDocument.Parse(responseString);
        Assert.True(result.RootElement.TryGetProperty("data", out var data), "Response should contain data");
        Assert.True(data.TryGetProperty("getTestItems", out var items), "Data should contain getTestItems");
        
        var itemCount = items.GetArrayLength();
        Assert.True(itemCount <= 2, $"Should respect take parameter, got {itemCount} items");
        
        _output.WriteLine($"✅ Query parameters working correctly: {itemCount} items returned");
    }

    [Fact]
    public async Task GraphQL_Error_Handling_Should_Work_With_Invalid_Field()
    {
        // Arrange - Invalid field name
        var invalidQuery = @"
            query {
              nonExistentField {
                id
              }
            }";

        var request = new { query = invalidQuery };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"Error Handling Response Status: {response.StatusCode}");
        _output.WriteLine($"Error Handling Response: {responseString}");

        // Assert
        // GraphQL can return either 200 with errors in body OR 400 for validation errors
        // Both are valid according to GraphQL spec implementations
        var isValidResponse = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest;
        Assert.True(isValidResponse, $"GraphQL should return 200 or 400 for query errors, got {response.StatusCode}");
        
        var result = JsonDocument.Parse(responseString);
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors), "Response should contain errors");
        Assert.True(errors.GetArrayLength() > 0, "Should have at least one error");
        
        var firstError = errors[0];
        Assert.True(firstError.TryGetProperty("message", out _), "Error should have message");
        
        _output.WriteLine("✅ Error handling working correctly");
    }

    [Fact]
    public async Task GraphQL_Performance_Should_Be_Acceptable_With_Mock_Data()
    {
        // Arrange
        var query = @"
            query {
              getTestItems(take: 10) {
                id
                name
                description
                createdAt
                isActive
              }
            }";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.PostAsync("/graphql", content);
        stopwatch.Stop();

        var responseString = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Performance Test took: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Performance Test Response: {responseString}");

        // Performance should be very fast with mock data
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Mock query took {stopwatch.ElapsedMilliseconds}ms, should be under 1000ms");
        Assert.True(response.IsSuccessStatusCode, $"Performance test failed with status: {response.StatusCode}");
        
        _output.WriteLine($"✅ Performance acceptable: {stopwatch.ElapsedMilliseconds}ms");
    }
}
