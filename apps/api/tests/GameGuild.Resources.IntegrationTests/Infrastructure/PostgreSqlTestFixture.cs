using Testcontainers.PostgreSql;
using Xunit;

namespace GameGuild.Resources.IntegrationTests.Infrastructure;

/// <summary>
/// Shared fixture that manages a PostgreSQL container for integration tests.
/// This fixture is shared across all test classes in the same collection,
/// reducing container startup overhead.
/// </summary>
public class PostgreSqlTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    
    public string ConnectionString => _container.GetConnectionString();
    public bool IsRunning { get; private set; }

    public PostgreSqlTestFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("gameguild_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        IsRunning = true;
        
        // Note: We don't call EnsureCreatedAsync() here because the application's
        // EF Core model has complex configurations that require the full application
        // context to resolve. The WebApplicationFactory will handle database setup
        // when the app starts.
        // 
        // If you need to create the schema, consider:
        // 1. Using migrations: context.Database.MigrateAsync()
        // 2. Or ensuring the main app's model is properly configured
    }

    public async Task DisposeAsync()
    {
        IsRunning = false;
        await _container.DisposeAsync();
    }
}

/// <summary>
/// Collection definition for tests that share a PostgreSQL container.
/// All test classes with [Collection("PostgreSql")] will share the same container instance.
/// </summary>
[CollectionDefinition("PostgreSql")]
public class PostgreSqlCollectionDefinition : ICollectionFixture<PostgreSqlTestFixture>
{
}
