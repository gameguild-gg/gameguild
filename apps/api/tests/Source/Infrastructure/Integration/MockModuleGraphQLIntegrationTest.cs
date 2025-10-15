using System.Text;
using System.Text.Json;
using GameGuild.Tests.Fixtures;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Integration;

/// <summary>
/// Test the mock module GraphQL integration to verify infrastructure works
/// </summary>
public class MockModuleGraphQLIntegrationTest : IClassFixture<MockModuleTestServerFixture> {
  private readonly MockModuleTestServerFixture _fixture;
  private readonly HttpClient _client;
  private readonly ITestOutputHelper _output;

  public MockModuleGraphQLIntegrationTest(MockModuleTestServerFixture fixture, ITestOutputHelper output) {
    _fixture = fixture;
    _client = _fixture.CreateClient();
    _output = output;
  }

  [Fact]
  public async Task GraphQL_Mock_Module_Should_Be_Available() {
    // Test the simple non-CQRS method first
    // First, let's see what fields are actually available with introspection
    var introspectionQuery = @"
            query {
                __type(name: ""Query"") {
                    fields {
                        name
                        type {
                            name
                        }
                    }
                }
            }";

    var introspectionRequest = new { query = introspectionQuery };
    var introspectionContent = new StringContent(
        JsonSerializer.Serialize(introspectionRequest),
        Encoding.UTF8,
        "application/json"
    );

    var introspectionResponse = await _client.PostAsync("/graphql", introspectionContent);
    var introspectionString = await introspectionResponse.Content.ReadAsStringAsync();

    _output.WriteLine($"Schema Introspection Status: {introspectionResponse.StatusCode}");
    _output.WriteLine($"Schema Introspection Response: {introspectionString}");

    // Test our simple getTestMessage query (doesn't need complex dependencies)
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

    _output.WriteLine($"Mock Module Response Status: {response.StatusCode}");
    _output.WriteLine($"Mock Module Response: {responseString}");

    Assert.True(response.IsSuccessStatusCode, $"GraphQL request failed with status: {response.StatusCode}");

    var result = JsonDocument.Parse(responseString);

    // Verify no errors
    if (result.RootElement.TryGetProperty("errors", out var errors)) {
      var errorMessages = errors.EnumerateArray().Select(e => e.GetProperty("message").GetString());
      Assert.True(false, $"GraphQL errors: {string.Join(", ", errorMessages)}");
    }

    // Verify the mock module field is available
    var data = result.RootElement.GetProperty("data");
    var message = data.GetProperty("getTestMessage").GetString();

    Assert.Equal("Hello from GraphQL infrastructure test!", message);
    _output.WriteLine("✅ Mock module GraphQL integration working");
  }

  [Fact]
  public async Task GraphQL_Mock_CQRS_Integration_Should_Work() {
    // Test the CQRS pattern with GetTestItems
    var query = @"
            query {
              getTestItems(take: 3) {
                id
                name
                description
                isActive
                createdAt
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

    _output.WriteLine($"CQRS Test Response Status: {response.StatusCode}");
    _output.WriteLine($"CQRS Test Response: {responseString}");

    Assert.True(response.IsSuccessStatusCode, $"GraphQL request failed with status: {response.StatusCode}");

    var result = JsonDocument.Parse(responseString);

    // Verify no errors
    if (result.RootElement.TryGetProperty("errors", out var errors)) {
      var errorMessages = errors.EnumerateArray().Select(e => e.GetProperty("message").GetString());
      Assert.True(false, $"GraphQL errors: {string.Join(", ", errorMessages)}");
    }

    // Verify CQRS pattern worked
    var data = result.RootElement.GetProperty("data");
    var testItems = data.GetProperty("getTestItems");
    Assert.True(testItems.ValueKind == JsonValueKind.Array);

    // Mock handler should return some test data
    var items = testItems.EnumerateArray().ToList();
    Assert.True(items.Count > 0, "Mock CQRS handler should return test items");
  }
}
