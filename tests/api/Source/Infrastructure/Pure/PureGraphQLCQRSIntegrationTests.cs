using System.Reflection;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Pure;

/// <summary>
/// Tests that validate GraphQL→CQRS integration using pure test modules
/// No business logic or real modules - only infrastructure testing
/// </summary>
public class PureGraphQLCQRSIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PureGraphQLCQRSIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Pure_GraphQL_CQRS_Should_Execute_Simple_Query()
    {
        // Arrange
        var server = CreatePureTestServer();
        var client = server.CreateClient();

        var query = @"
        {
            getPureTestMessage
        }";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"GraphQL request failed: {response.StatusCode}, {responseString}");

        var jsonDoc = JsonDocument.Parse(responseString);
        Assert.True(jsonDoc.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("getPureTestMessage", out var message));
        Assert.Equal("Hello from pure infrastructure test!", message.GetString());

        _output.WriteLine($"✅ Pure GraphQL→CQRS simple query works: {message.GetString()}");
    }

    [Fact]
    public async Task Pure_GraphQL_CQRS_Should_Execute_Collection_Query()
    {
        // Arrange
        var server = CreatePureTestServer();
        var client = server.CreateClient();

        var query = @"
        {
            getPureTestItems(take: 2) {
                id
                name
                createdAt
            }
        }";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"GraphQL request failed: {response.StatusCode}, {responseString}");

        var jsonDoc = JsonDocument.Parse(responseString);
        Assert.True(jsonDoc.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("getPureTestItems", out var items));
        Assert.True(items.GetArrayLength() == 2);

        var firstItem = items[0];
        Assert.True(firstItem.TryGetProperty("id", out var id));
        Assert.True(firstItem.TryGetProperty("name", out var name));
        Assert.Equal("test-1", id.GetString());
        Assert.Equal("Pure Test Item 1", name.GetString());

        _output.WriteLine($"✅ Pure GraphQL→CQRS collection query works: {items.GetArrayLength()} items returned");
    }

    [Fact]
    public async Task Pure_GraphQL_Should_Handle_Invalid_Query_Gracefully()
    {
        // Arrange
        var server = CreatePureTestServer();
        var client = server.CreateClient();

        var query = @"
        {
            nonExistentField
        }";

        var request = new { query = query };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        // Assert
        // GraphQL can return either 200 with errors or 400 for invalid queries - both are valid
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.BadRequest,
                   $"GraphQL should return 200 or 400 for invalid queries, got {response.StatusCode}");

        var jsonDoc = JsonDocument.Parse(responseString);
        Assert.True(jsonDoc.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.GetArrayLength() > 0);

        _output.WriteLine($"✅ Pure GraphQL handles invalid queries gracefully with {errors.GetArrayLength()} errors");
    }

    [Fact]
    public async Task Pure_GraphQL_Schema_Introspection_Should_Work()
    {
        // Arrange
        var server = CreatePureTestServer();
        var client = server.CreateClient();

        var query = @"
        {
            __schema {
                queryType {
                    name
                    fields {
                        name
                        type {
                            name
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

        // Act
        var response = await client.PostAsync("/graphql", content);
        var responseString = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Schema introspection failed: {response.StatusCode}, {responseString}");

        var jsonDoc = JsonDocument.Parse(responseString);
        Assert.True(jsonDoc.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("__schema", out var schema));
        Assert.True(schema.TryGetProperty("queryType", out var queryType));
        Assert.True(queryType.TryGetProperty("name", out var typeName));
        Assert.Equal("Query", typeName.GetString());

        _output.WriteLine("✅ Pure GraphQL schema introspection works");
    }

    [Fact]
    public void Pure_MediatR_Handlers_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateMinimalConfiguration();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"PureHandlers_{Guid.NewGuid()}"));

        // Add MediatR with pure test handlers (using older pattern from API)
        services.AddMediatR(Assembly.GetExecutingAssembly());

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Assert
        Assert.NotNull(mediator);

        // Verify we can resolve our test handlers
        var testMessageHandler = serviceProvider.GetService<IRequestHandler<GetPureTestMessageQuery, string>>();
        var testItemsHandler = serviceProvider.GetService<IRequestHandler<GetPureTestItemsQuery, IEnumerable<PureTestItem>>>();

        Assert.NotNull(testMessageHandler);
        Assert.NotNull(testItemsHandler);

        _output.WriteLine("✅ Pure MediatR handlers registered correctly");
    }

    private TestServer CreatePureTestServer()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    var configuration = CreateMinimalConfiguration();
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddLogging();
                    services.AddRouting(); // Add routing services

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase($"PureTest_{Guid.NewGuid()}"));

                    // Add MediatR with our pure test handlers (using older pattern from API)
                    services.AddMediatR(Assembly.GetExecutingAssembly());

                    // Register our test handlers explicitly
                    services.AddScoped<IRequestHandler<GetPureTestMessageQuery, string>, GetPureTestMessageHandler>();
                    services.AddScoped<IRequestHandler<GetPureTestItemsQuery, IEnumerable<PureTestItem>>, GetPureTestItemsHandler>();

                    // Add GraphQL server with our pure test types
                    services.AddGraphQLServer()
                        .AddQueryType<PureTestQuery>() // Use our simple query type instead
                        .AddType<PureTestItemType>()
                        .ModifyRequestOptions(opt => 
                        {
                            opt.IncludeExceptionDetails = true;
                        })
                        .DisableIntrospection(false); // Enable introspection for tests
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGraphQL("/graphql");
                    });
                });
            });

        var host = hostBuilder.Start();
        return host.GetTestServer();
    }

    private static IConfiguration CreateMinimalConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Database:Provider"] = "InMemory",
            ["Database:ConnectionString"] = "InMemory",
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}
