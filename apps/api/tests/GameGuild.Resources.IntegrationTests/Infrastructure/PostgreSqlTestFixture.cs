using Npgsql;
using GameGuild.TestSupport.Economy;
using Xunit;

namespace GameGuild.Resources.IntegrationTests.Infrastructure;

/// <summary>
/// Shared fixture that manages a PostgreSQL container for integration tests.
/// This fixture is shared across all test classes in the same collection,
/// reducing container startup overhead.
/// </summary>
public class PostgreSqlTestFixture : IAsyncLifetime
{
    private EconomyPostgreSqlTestDatabase _container = null!;
    
    public string ConnectionString => _container.ConnectionString;
    public bool IsRunning { get; private set; }

    public async Task InitializeAsync()
    {
        _container = await EconomyPostgreSqlTestDatabase.CreateAsync("resources_integration");
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

    public async Task<PostgreSqlTestDatabase> CreateDatabaseAsync(string prefix)
    {
        var safePrefix = new string(prefix
            .Where(character => char.IsLetterOrDigit(character) || character == '_')
            .Select(char.ToLowerInvariant)
            .ToArray());
        var databaseName = $"{safePrefix}_{Guid.NewGuid():N}";

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = databaseName,
            Pooling = false
        };

        return new PostgreSqlTestDatabase(ConnectionString, connectionStringBuilder.ConnectionString, databaseName);
    }
}

public sealed class PostgreSqlTestDatabase(
    string adminConnectionString,
    string connectionString,
    string databaseName) : IAsyncDisposable
{
    public string ConnectionString { get; } = connectionString;

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using (var terminateConnections = connection.CreateCommand())
        {
            terminateConnections.CommandText =
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid()";
            terminateConnections.Parameters.AddWithValue("databaseName", databaseName);
            await terminateConnections.ExecuteNonQueryAsync();
        }

        await using var dropDatabase = connection.CreateCommand();
        dropDatabase.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
        await dropDatabase.ExecuteNonQueryAsync();
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
