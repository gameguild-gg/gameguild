using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.TestSupport.Economy;

public sealed class EconomyPostgreSqlTestDatabase : IAsyncDisposable
{
    private readonly string? _adminConnectionString;
    private readonly string? _databaseName;
    private readonly PostgreSqlContainer? _container;
    private readonly bool _dropDatabaseOnDispose;

    private EconomyPostgreSqlTestDatabase(
        string connectionString,
        string? adminConnectionString,
        string? databaseName,
        PostgreSqlContainer? container,
        bool dropDatabaseOnDispose)
    {
        ConnectionString = connectionString;
        _adminConnectionString = adminConnectionString;
        _databaseName = databaseName;
        _container = container;
        _dropDatabaseOnDispose = dropDatabaseOnDispose;
    }

    public string ConnectionString { get; }

    public static async Task<EconomyPostgreSqlTestDatabase> CreateAsync(
        string databasePrefix,
        CancellationToken cancellationToken = default)
    {
        var gateConnectionString = Environment.GetEnvironmentVariable("ECONOMY_POSTGRES_CONNECTION");

        if (!string.IsNullOrWhiteSpace(gateConnectionString))
        {
            return await CreateFromGateAsync(
                    gateConnectionString,
                    CreateGateDatabaseName(databasePrefix),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var databaseName = CreateDatabaseName(databasePrefix);

        var container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase(databaseName)
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync(cancellationToken).ConfigureAwait(false);
        var databaseBuilder = new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Pooling = false };
        var adminBuilder = new NpgsqlConnectionStringBuilder(databaseBuilder.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };

        return new EconomyPostgreSqlTestDatabase(
            databaseBuilder.ConnectionString,
            adminBuilder.ConnectionString,
            databaseBuilder.Database,
            container,
            false);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_adminConnectionString) || string.IsNullOrWhiteSpace(_databaseName))
        {
            throw new InvalidOperationException("The PostgreSQL test database cannot be reset without an administrative connection.");
        }

        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await DropDatabaseAsync(connection, _databaseName, cancellationToken).ConfigureAwait(false);
        await using var createDatabase = connection.CreateCommand();
        createDatabase.CommandText = $"CREATE DATABASE \"{_databaseName}\";";
        await createDatabase.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            return;
        }

        if (!_dropDatabaseOnDispose || string.IsNullOrWhiteSpace(_adminConnectionString) || string.IsNullOrWhiteSpace(_databaseName))
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await DropDatabaseAsync(connection, _databaseName, CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task<EconomyPostgreSqlTestDatabase> CreateFromGateAsync(
        string gateConnectionString,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(gateConnectionString) { Pooling = false };
        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var databaseExists = connection.CreateCommand();
        databaseExists.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName;";
        databaseExists.Parameters.AddWithValue("databaseName", databaseName);
        var exists = await databaseExists.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
        if (!exists)
        {
            await using var createDatabase = connection.CreateCommand();
            createDatabase.CommandText = $"CREATE DATABASE \"{databaseName}\";";
            await createDatabase.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var databaseBuilder = new NpgsqlConnectionStringBuilder(adminBuilder.ConnectionString)
        {
            Database = databaseName,
            Pooling = false
        };
        return new EconomyPostgreSqlTestDatabase(
            databaseBuilder.ConnectionString,
            adminBuilder.ConnectionString,
            databaseName,
            null,
            true);
    }

    private static string CreateDatabaseName(string databasePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePrefix);
        var normalized = new string(databasePrefix
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == '_')
            .Select(char.ToLowerInvariant)
            .ToArray());
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Database prefix must contain a letter, digit, or underscore.", nameof(databasePrefix));
        }

        var prefix = normalized[..Math.Min(normalized.Length, 22)];
        return $"economy_{prefix}_{Guid.NewGuid():N}";
    }

    private static string CreateGateDatabaseName(string databasePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePrefix);
        var normalized = new string(databasePrefix
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == '_')
            .Select(char.ToLowerInvariant)
            .ToArray());
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Database prefix must contain a letter, digit, or underscore.", nameof(databasePrefix));
        }

        var prefix = normalized[..Math.Min(normalized.Length, 22)];
        return $"economy_{prefix}_{Guid.NewGuid():N}";
    }

    private static async Task DropDatabaseAsync(
        NpgsqlConnection connection,
        string databaseName,
        CancellationToken cancellationToken)
    {
        await using var terminateConnections = connection.CreateCommand();
        terminateConnections.CommandText =
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid();";
        terminateConnections.Parameters.AddWithValue("databaseName", databaseName);
        await terminateConnections.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await using var dropDatabase = connection.CreateCommand();
        dropDatabase.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\";";
        await dropDatabase.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
