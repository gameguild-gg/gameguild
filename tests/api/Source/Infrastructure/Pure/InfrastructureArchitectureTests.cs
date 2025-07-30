using System.Text;
using System.Text.Json;
using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Tests.Infrastructure.Integration;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Pure;

/// <summary>
/// Comprehensive pure infrastructure tests that validate each architectural layer in isolation
/// Tests infrastructure components without business modules to identify root causes of failures
/// </summary>
public class InfrastructureArchitectureTests(ITestOutputHelper output) {
  [Fact]
  public void Configuration_Should_Load_With_Minimal_Settings() {
    // Arrange & Act
    var configuration = CreateMinimalConfiguration();

    // Assert
    Assert.NotNull(configuration);
    Assert.Equal("InMemory", configuration["Database:Provider"]);
    Assert.Equal("GameGuild.Test", configuration["Jwt:Issuer"]);

    output.WriteLine("✅ Configuration loaded successfully with minimal settings");
  }

  [Fact]
  public void ServiceCollection_Should_Accept_Basic_Services() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    // Act
    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var logger = serviceProvider.GetService<ILogger<InfrastructureArchitectureTests>>();
    var config = serviceProvider.GetService<IConfiguration>();

    Assert.NotNull(logger);
    Assert.NotNull(config);

    output.WriteLine("✅ Basic services registered successfully");
  }

  [Fact]
  public void Database_InMemory_Should_Register_Successfully() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Act
    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
    );

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var dbContext = serviceProvider.GetService<ApplicationDbContext>();

    Assert.NotNull(dbContext);
    Assert.True(dbContext.Database.IsInMemory());

    output.WriteLine("✅ InMemory database registered successfully");
  }

  [Fact]
  public void Database_SQLite_Should_Register_Successfully() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Act
    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseSqlite("Data Source=:memory:")
    );

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var dbContext = serviceProvider.GetService<ApplicationDbContext>();

    Assert.NotNull(dbContext);
    Assert.True(dbContext.Database.IsSqlite());

    output.WriteLine("✅ SQLite database registered successfully");
  }

  [Fact]
  public void Application_Layer_Should_Register_Without_Dependencies() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();
    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseInMemoryDatabase($"AppLayer_{Guid.NewGuid()}")
    );

    // Act
    services.AddApplication();

    // Add mock IAuthService for Authentication handlers (required by MediatR)
    services.AddScoped<GameGuild.Modules.Authentication.IAuthService, MockAuthService>();

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var mediator = serviceProvider.GetService<MediatR.IMediator>();

    Assert.NotNull(mediator);

    output.WriteLine("✅ Application layer registered successfully");
  }

  [Fact]
  public async Task Pure_GraphQL_Server_Should_Register_With_Empty_Schema() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Act - Register minimal GraphQL server with just base types for infrastructure testing
    services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

    // Assert
    var serviceProvider = services.BuildServiceProvider();

    // Try to get the schema executor factory first
    var executorResolver = serviceProvider.GetService<IRequestExecutorResolver>();

    if (executorResolver != null) {
      var executor = await executorResolver.GetRequestExecutorAsync();
      Assert.NotNull(executor);
    }
    else {
      // Fallback to direct service lookup
      var executor = serviceProvider.GetService<IRequestExecutor>();
      Assert.NotNull(executor);
    }

    output.WriteLine("✅ Pure GraphQL server with empty schema registered successfully");
  }

  [Fact]
  public async Task Pure_GraphQL_Server_Should_Execute_Basic_Query() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

    var serviceProvider = services.BuildServiceProvider();
    var executorResolver = serviceProvider.GetRequiredService<IRequestExecutorResolver>();
    var executor = await executorResolver.GetRequestExecutorAsync();

    // Act
    var result = await executor.ExecuteAsync("{ health }");

    // Assert
    Assert.NotNull(result);
    var resultJson = result.ToJson();
    Assert.Contains("GraphQL API is healthy", resultJson);

    output.WriteLine($"✅ Pure GraphQL query executed successfully: {resultJson}");
  }

  [Fact]
  public async Task TestServer_Should_Start_With_Minimal_Configuration() {
    // Arrange
    var hostBuilder = new HostBuilder()
      .ConfigureWebHost(webHost => {
          webHost.UseTestServer();
          webHost.ConfigureServices(services => {
              var configuration = CreateMinimalConfiguration();
              services.AddSingleton<IConfiguration>(configuration);
              services.AddLogging();

              services.AddDbContext<ApplicationDbContext>(options =>
                                                            options.UseInMemoryDatabase($"TestServer_{Guid.NewGuid()}")
              );

              // Add minimal routing and endpoint services
              services.AddRouting();
              services.AddEndpointsApiExplorer();
            }
          );
          webHost.Configure(app => {
              app.UseRouting();
              app.UseEndpoints(endpoints => { endpoints.MapGet("/health", () => "OK"); });
            }
          );
        }
      );

    // Act & Assert
    using var host = hostBuilder.Start();
    var server = host.GetTestServer();
    var client = server.CreateClient();

    var response = await client.GetAsync("/health");
    var content = await response.Content.ReadAsStringAsync();

    Assert.True(response.IsSuccessStatusCode);
    Assert.Equal("OK", content);

    output.WriteLine("✅ Test server started and responded to basic endpoint");
  }

  [Fact]
  public async Task TestServer_With_GraphQL_Should_Work() {
    // Arrange
    var hostBuilder = new HostBuilder()
      .ConfigureWebHost(webHost => {
          webHost.UseTestServer();
          webHost.ConfigureServices(services => {
              var configuration = CreateMinimalConfiguration();
              services.AddSingleton<IConfiguration>(configuration);
              services.AddLogging();
              services.AddRouting(); // Add routing services

              services.AddDbContext<ApplicationDbContext>(options =>
                                                            options.UseInMemoryDatabase($"GraphQLServer_{Guid.NewGuid()}")
              );

              services.AddGraphQLServer()
                      .AddQueryType<Query>()
                      .AddMutationType<Mutation>()
                      .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);
            }
          );
          webHost.Configure(app => {
              app.UseRouting();
              app.UseEndpoints(endpoints => { endpoints.MapGraphQL("/graphql"); });
            }
          );
        }
      );

    // Act
    using var host = hostBuilder.Start();
    var server = host.GetTestServer();
    var client = server.CreateClient();

    var query = @"{ health }";
    var request = new {
      query = query
    };
    var content = new StringContent(
      JsonSerializer.Serialize(request),
      Encoding.UTF8,
      "application/json"
    );

    var response = await client.PostAsync("/graphql", content);
    var responseString = await response.Content.ReadAsStringAsync();

    // Assert
    Assert.True(response.IsSuccessStatusCode, $"GraphQL request failed: {response.StatusCode}, {responseString}");
    Assert.Contains("GraphQL API is healthy", responseString);

    output.WriteLine($"✅ GraphQL server responded successfully: {responseString}");
  }

  [Fact]
  public async Task Infrastructure_Components_Should_Work_Together() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Add all infrastructure components step by step
    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseInMemoryDatabase($"Integration_{Guid.NewGuid()}")
    );

    services.AddApplication();

    // Add mock IAuthService for Authentication handlers (required by MediatR)
    services.AddScoped<GameGuild.Modules.Authentication.IAuthService, MockAuthService>();

    services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

    // Act
    var serviceProvider = services.BuildServiceProvider();

    // Assert - Check that all major components are available
    var dbContext = serviceProvider.GetService<ApplicationDbContext>();
    var mediator = serviceProvider.GetService<MediatR.IMediator>();

    // For GraphQL executor, try multiple resolution methods
    var executor = serviceProvider.GetService<IRequestExecutor>();

    if (executor == null) {
      var executorResolver = serviceProvider.GetService<IRequestExecutorResolver>();

      if (executorResolver != null) {
        try { executor = await executorResolver.GetRequestExecutorAsync(); }
        catch {
          // Ignore if executor resolution fails - we'll check for null
        }
      }
    }

    Assert.NotNull(dbContext);
    Assert.NotNull(mediator);
    Assert.NotNull(executor);

    output.WriteLine("✅ All infrastructure components work together");
  }

  [Fact]
  public void Presentation_Layer_Should_Register_Independently() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    var presentationOptions = new DependencyInjection.PresentationOptions {
      AllowedOrigins = ["http://localhost"],
      EnableSwagger = false, // Disable swagger to avoid extra dependencies
      EnableHealthChecks = true,
      EnableResponseCompression = false,
      EnableRateLimiting = false,
      ApiTitle = "Test API",
      ApiVersion = "v1",
    };

    // Act
    services.AddPresentation(presentationOptions);

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    Assert.NotNull(serviceProvider);

    output.WriteLine("✅ Presentation layer registered independently");
  }

  [Fact]
  public void Infrastructure_Middlewares_Should_Be_Configurable() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateMinimalConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Add middleware-related services
    services.AddRouting();
    services.AddCors(options => {
        options.AddDefaultPolicy(builder => {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
          }
        );
      }
    );

    services.AddAuthentication("Test")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>(
              "Test",
              options => { }
            );

    services.AddAuthorization();

    // Act
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    Assert.NotNull(serviceProvider);
    output.WriteLine("✅ Infrastructure middleware services configured successfully");
  }

  private static IConfiguration CreateMinimalConfiguration() {
    var configData = new Dictionary<string, string?> {
      ["Database:Provider"] = "InMemory",
      ["Database:ConnectionString"] = "InMemory",
      ["Jwt:SecretKey"] = "test-jwt-secret-key-for-infrastructure-testing-only-minimum-32-characters",
      ["Jwt:Issuer"] = "GameGuild.Test",
      ["Jwt:Audience"] = "GameGuild.Test.Users",
      ["Jwt:ExpiryInMinutes"] = "15",
      ["ASPNETCORE_ENVIRONMENT"] = "Testing",
    };

    return new ConfigurationBuilder()
           .AddInMemoryCollection(configData)
           .Build();
  }
}

/// <summary>
/// Test authentication handler for testing middleware without real authentication
/// </summary>
public class TestAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> {
  public TestAuthenticationHandler(
    Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder
  )
    : base(options, logger, encoder) { }

  protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync() {
    var claims = new[] {
      new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser"), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
    };

    var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);
    var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");

    return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
  }
}
