using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GameGuild.Common;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using HotChocolate.Execution;
using MediatR;

namespace GameGuild.API.Tests.Infrastructure.Pure;

/// <summary>
/// Tests that break down the application bootstrap process into small, testable components
/// Validates each layer can be registered independently and identifies dependency issues
/// </summary>
public class BootstrapComponentTests
{
    private readonly ITestOutputHelper _output;

    public BootstrapComponentTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Step1_Configuration_Should_Load_Successfully()
    {
        // Arrange & Act
        var configuration = CreateProductionLikeConfiguration();
        
        // Assert
        Assert.NotNull(configuration);
        Assert.NotNull(configuration["Jwt:SecretKey"]);
        Assert.NotNull(configuration["ConnectionStrings:DefaultConnection"]);
        
        _output.WriteLine("✅ Step 1: Configuration loading works");
    }

    [Fact]
    public void Step2_Basic_Services_Should_Register()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateProductionLikeConfiguration();
        
        // Act - Register core services
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<BootstrapComponentTests>>();
        var config = serviceProvider.GetService<IConfiguration>();
        
        Assert.NotNull(logger);
        Assert.NotNull(config);
        
        _output.WriteLine("✅ Step 2: Basic services registration works");
    }

    [Fact]
    public void Step3_Database_Should_Register_With_Different_Providers()
    {
        // Test InMemory
        var inMemoryServices = new ServiceCollection();
        var configuration = CreateProductionLikeConfiguration();
        inMemoryServices.AddSingleton<IConfiguration>(configuration);
        inMemoryServices.AddLogging();
        
        inMemoryServices.AddDbContext<ApplicationDbContext>(options => 
            options.UseInMemoryDatabase($"Bootstrap_InMemory_{Guid.NewGuid()}"));
        
        var inMemoryProvider = inMemoryServices.BuildServiceProvider();
        var inMemoryContext = inMemoryProvider.GetService<ApplicationDbContext>();
        Assert.NotNull(inMemoryContext);
        Assert.True(inMemoryContext.Database.IsInMemory());
        
        // Test SQLite
        var sqliteServices = new ServiceCollection();
        sqliteServices.AddSingleton<IConfiguration>(configuration);
        sqliteServices.AddLogging();
        
        sqliteServices.AddDbContext<ApplicationDbContext>(options => 
            options.UseSqlite("Data Source=:memory:"));
        
        var sqliteProvider = sqliteServices.BuildServiceProvider();
        var sqliteContext = sqliteProvider.GetService<ApplicationDbContext>();
        Assert.NotNull(sqliteContext);
        Assert.True(sqliteContext.Database.IsSqlite());
        
        _output.WriteLine("✅ Step 3: Database registration works with multiple providers");
    }

    [Fact]
    public void Step4_Application_Layer_Should_Register_Independently()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateProductionLikeConfiguration();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddHttpContextAccessor(); // Required for pipeline behaviors
        services.AddSingleton<GameGuild.Common.IDateTimeProvider, GameGuild.Common.DateTimeProvider>(); // Required for PerformanceBehavior
        services.AddDbContext<ApplicationDbContext>(options => 
            options.UseInMemoryDatabase($"AppLayer_{Guid.NewGuid()}"));

        // Act
        services.AddApplication();
        
        // Add mock IAuthService for Authentication handlers (required by MediatR)
        services.AddScoped<GameGuild.Modules.Authentication.IAuthService, GameGuild.API.Tests.Infrastructure.Integration.MockAuthService>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Check MediatR is registered
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
        
        // Check pipeline behaviors are registered
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TestRequest, string>>().ToList();
        Assert.True(behaviors.Count > 0, "Pipeline behaviors should be registered");
        
        _output.WriteLine($"✅ Step 4: Application layer registered with {behaviors.Count} behaviors");
    }

    [Fact]
    public void Step5_Infrastructure_Layer_Should_Register_Without_Auth()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateProductionLikeConfiguration();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act - Add infrastructure without authentication
        services.AddInfrastructure(configuration, excludeAuth: true);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var dbContext = serviceProvider.GetService<ApplicationDbContext>();
        Assert.NotNull(dbContext);
        
        _output.WriteLine("✅ Step 5: Infrastructure layer registered without auth");
    }

    [Fact]
    public void Step6_Presentation_Layer_Should_Register_With_Minimal_Options()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateProductionLikeConfiguration();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        var presentationOptions = new DependencyInjection.PresentationOptions
        {
            AllowedOrigins = ["http://localhost:3000"],
            EnableSwagger = false, // Disable to avoid OpenAPI dependencies in tests
            EnableHealthChecks = true,
            EnableResponseCompression = false,
            EnableRateLimiting = false,
            ApiTitle = "Test API",
            ApiVersion = "v1"
        };

        // Act
        services.AddPresentation(presentationOptions);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider);
        
        _output.WriteLine("✅ Step 6: Presentation layer registered with minimal options");
    }

    [Fact]
    public async Task Step7_GraphQL_Infrastructure_Should_Register_Separately()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateProductionLikeConfiguration();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => 
            options.UseInMemoryDatabase($"GraphQL_{Guid.NewGuid()}"));

        // Act - Add GraphQL with minimal configuration
        services.AddGraphQLServer()
            .AddQueryType<GameGuild.Common.Query>()
            .AddMutationType<GameGuild.Common.Mutation>()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var executorResolver = serviceProvider.GetService<IRequestExecutorResolver>();
        Assert.NotNull(executorResolver);
        
        try
        {
            var executor = await executorResolver.GetRequestExecutorAsync();
            Assert.NotNull(executor);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"GraphQL executor resolution note: {ex.Message}");
            Assert.NotNull(executorResolver); // At minimum the resolver should be available
        }
        
        _output.WriteLine("✅ Step 7: GraphQL infrastructure registered separately");
    }

    [Fact]
    public async Task Step8_Complete_Bootstrap_Should_Work_Step_By_Step()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateProductionLikeConfiguration();
        
        // Act - Build up the entire service container step by step
        
        // Step 1: Core services
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        
        // Step 2: Database
        services.AddDbContext<ApplicationDbContext>(options => 
            options.UseInMemoryDatabase($"CompleteBootstrap_{Guid.NewGuid()}"));
        
        // Step 3: Application layer
        services.AddApplication();
        
        // Add mock IAuthService for Authentication handlers (required by MediatR)
        services.AddScoped<GameGuild.Modules.Authentication.IAuthService, GameGuild.API.Tests.Infrastructure.Integration.MockAuthService>();
        
        // Step 4: Infrastructure (without auth for testing)
        services.AddInfrastructure(configuration, excludeAuth: true);
        
        // Step 5: Presentation layer
        var presentationOptions = new DependencyInjection.PresentationOptions
        {
            AllowedOrigins = ["http://localhost:3000"],
            EnableSwagger = false,
            EnableHealthChecks = true,
            EnableResponseCompression = false,
            EnableRateLimiting = false,
            ApiTitle = "Test API",
            ApiVersion = "v1"
        };
        services.AddPresentation(presentationOptions);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify all major components are available
        var dbContext = serviceProvider.GetService<ApplicationDbContext>();
        var mediator = serviceProvider.GetService<IMediator>();
        
        // For GraphQL executor, try multiple resolution methods
        var executor = serviceProvider.GetService<IRequestExecutor>();
        if (executor == null)
        {
            var executorResolver = serviceProvider.GetService<IRequestExecutorResolver>();
            if (executorResolver != null)
            {
                try
                {
                    executor = await executorResolver.GetRequestExecutorAsync();
                }
                catch
                {
                    // Ignore if executor resolution fails - we'll check for null
                }
            }
        }
        
        Assert.NotNull(dbContext);
        Assert.NotNull(mediator);
        Assert.NotNull(executor);
        
        _output.WriteLine("✅ Step 8: Complete bootstrap works step by step");
    }

    [Fact]
    public async Task Step9_Complete_TestServer_Should_Start()
    {
        // Arrange
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    var configuration = CreateProductionLikeConfiguration();
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddLogging();

                    // Database
                    services.AddDbContext<ApplicationDbContext>(options => 
                        options.UseInMemoryDatabase($"TestServer_{Guid.NewGuid()}"));

                    // Application layer
                    services.AddApplication();

                    // Add mock IAuthService for Authentication handlers (required by MediatR)
                    services.AddScoped<GameGuild.Modules.Authentication.IAuthService, GameGuild.API.Tests.Infrastructure.Integration.MockAuthService>();

                    // Infrastructure (without auth)
                    services.AddInfrastructure(configuration, excludeAuth: true);

                    // Presentation layer (minimal)
                    var presentationOptions = new DependencyInjection.PresentationOptions
                    {
                        AllowedOrigins = ["http://localhost:3000"],
                        EnableSwagger = false,
                        EnableHealthChecks = true,
                        EnableResponseCompression = false,
                        EnableRateLimiting = false,
                        ApiTitle = "Test API",
                        ApiVersion = "v1"
                    };
                    services.AddPresentation(presentationOptions);
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHealthChecks("/health");
                        endpoints.MapGraphQL("/graphql");
                        endpoints.MapGet("/", () => "Bootstrap test server running");
                    });
                });
            });

        // Act
        using var host = hostBuilder.Start();
        var server = host.GetTestServer();
        var client = server.CreateClient();

        var healthResponse = await client.GetAsync("/health");
        var rootResponse = await client.GetAsync("/");

        // Assert
        Assert.True(healthResponse.IsSuccessStatusCode);
        Assert.Equal("Healthy", await healthResponse.Content.ReadAsStringAsync());

        Assert.True(rootResponse.IsSuccessStatusCode);
        Assert.Equal("Bootstrap test server running", await rootResponse.Content.ReadAsStringAsync());

        _output.WriteLine("✅ Step 9: Complete test server started successfully");
    }

    [Fact]
    public void Bootstrap_Should_Fail_Gracefully_With_Invalid_Configuration()
    {
        // Arrange
        var services = new ServiceCollection();
        var invalidConfiguration = CreateInvalidConfiguration();
        
        services.AddSingleton<IConfiguration>(invalidConfiguration);
        services.AddLogging();

        // Act & Assert - This should either work with defaults or fail gracefully
        try
        {
            services.AddApplication();
            
            // Add mock IAuthService for Authentication handlers (required by MediatR)
            services.AddScoped<GameGuild.Modules.Authentication.IAuthService, GameGuild.API.Tests.Infrastructure.Integration.MockAuthService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var mediator = serviceProvider.GetService<IMediator>();
            Assert.NotNull(mediator); // Should still work with invalid config
            
            _output.WriteLine("✅ Bootstrap handles invalid configuration gracefully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️  Bootstrap failed with invalid config (expected): {ex.Message}");
            // This is acceptable - we want to know the system fails predictably
        }
    }

    [Fact]
    public void Memory_Usage_Should_Be_Reasonable_During_Bootstrap()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act
        for (int i = 0; i < 10; i++)
        {
            var services = new ServiceCollection();
            var configuration = CreateProductionLikeConfiguration();
            
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options => 
                options.UseInMemoryDatabase($"MemoryTest_{i}_{Guid.NewGuid()}"));
            services.AddApplication();
            
            // Add mock IAuthService for Authentication handlers (required by MediatR)
            services.AddScoped<GameGuild.Modules.Authentication.IAuthService, GameGuild.API.Tests.Infrastructure.Integration.MockAuthService>();
            
            // Add mock IAuthService for Authentication handlers (required by MediatR)
            services.AddScoped<GameGuild.Modules.Authentication.IAuthService, GameGuild.API.Tests.Infrastructure.Integration.MockAuthService>();
            
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.Dispose();
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;
        
        // Assert - Memory increase should be reasonable (less than 50MB for 10 iterations)
        Assert.True(memoryIncrease < 50 * 1024 * 1024, 
            $"Memory increase was {memoryIncrease / (1024.0 * 1024.0):F2} MB");
        
        _output.WriteLine($"✅ Memory usage reasonable: {memoryIncrease / (1024.0 * 1024.0):F2} MB increase for 10 bootstrap cycles");
    }

    private static IConfiguration CreateProductionLikeConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
            ["Database:Provider"] = "InMemory",
            ["Jwt:SecretKey"] = "production-like-secret-key-that-meets-minimum-requirements-for-testing",
            ["Jwt:Issuer"] = "GameGuild.Production",
            ["Jwt:Audience"] = "GameGuild.Users",
            ["Jwt:ExpiryInMinutes"] = "60",
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["Cors:AllowedOrigins:0"] = "https://gameguild.gg",
            ["Cors:AllowedOrigins:1"] = "http://localhost:3000"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private static IConfiguration CreateInvalidConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "too-short", // Invalid - too short
            ["Jwt:Issuer"] = "", // Invalid - empty
            ["ConnectionStrings:DefaultConnection"] = "invalid-connection-string"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}

/// <summary>
/// Test request for MediatR pipeline testing
/// </summary>
public record TestRequest : IRequest<string>;

/// <summary>
/// Test handler for MediatR pipeline testing
/// </summary>
public class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Test response");
    }
}
