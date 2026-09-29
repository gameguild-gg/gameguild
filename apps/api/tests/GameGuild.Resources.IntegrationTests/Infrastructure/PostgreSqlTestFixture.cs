using GameGuild.API.Database;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace GameGuild.Resources.IntegrationTests.Infrastructure;

/// <summary>
/// Shared fixture that manages an isolated PostgreSQL database for integration tests.
/// This fixture is shared across all test classes in the same collection,
/// using the gate-owned server in CI and a disposable container locally.
/// </summary>
public class PostgreSqlTestFixture : IAsyncLifetime
{
    private EconomyPostgreSqlTestDatabase _database = null!;
    
    public string ConnectionString => _database.ConnectionString;
    public bool IsRunning { get; private set; }

    public async Task InitializeAsync()
    {
        _database = await EconomyPostgreSqlTestDatabase.CreateAsync("resources_integration");
        // Resource tests create their own schema; do not inherit the full gate template.
        await _database.ResetAsync();
        // The full API's hosted preflight starts while WebApplicationFactory builds
        // its host. Its schema must already exist before any factory is constructed.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
        IsRunning = true;
        
        // Per-test quota databases created below remain empty so their focused
        // contexts can create only the schema required by each quota scenario.
    }

    public async Task DisposeAsync()
    {
        IsRunning = false;
        if (_database is not null)
            await _database.DisposeAsync();
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
/// Collection definition for tests that share an isolated PostgreSQL database.
/// All test classes with [Collection("PostgreSql")] share the same fixture instance.
/// </summary>
[CollectionDefinition("PostgreSql")]
public class PostgreSqlCollectionDefinition : ICollectionFixture<PostgreSqlTestFixture>
{
}
